using Application;
using Application.Interfaces;
using Application.Wrappers;
using Finbuckle.MultiTenant;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Infrastructure.Academics;
using Infrastructure.Constants;
using Infrastructure.Contexts;
using Infrastructure.Identity.Auth;
using Infrastructure.Identity.Models;
using Infrastructure.Identity.Services;
using Infrastructure.Identity.Tokens;
using Infrastructure.OpenApi;
using Infrastructure.Services;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.Generation.Processors.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace Infrastructure
{
    public static class Startup
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
        {
            return services
                .AddDbContext<TenantDbContext>(options => options.UseSqlServer(config.GetConnectionString("DefaultConnection")))
                .AddMultiTenant<AppTenantInfo>()
                .WithClaimStrategy(ClaimConstants.Tenant)
                .WithEFCoreStore<TenantDbContext, AppTenantInfo>()
                .Services
                .AddDbContext<ApplicationDbContext>(options => options
                .UseSqlServer(config.GetConnectionString("DefaultConnection")))
                .AddTransient<ITenantDbSeeder, TenantDbSeeder>()
                .AddTransient<ApplicationDbSeeder>()
                .AddTransient<IEmailService, EmailService>()
                .AddScoped<ITenantService, TenantService>()
                .AddScoped<IAdminService, AdminService>()
                .AddScoped<ISubscriptionService, SubscriptionService>()
                .AddHostedService<SubscriptionReminderService>()
                .AddScoped<ICenterService, CenterService>()
                .AddScoped<IAuthService, AuthService>()
                .AddScoped<ILinkService, LinkService>()
                .AddScoped<IGroupService, GroupService>()
                .AddScoped<IEnrollmentService, EnrollmentService>()
                .AddScoped<ISessionService, SessionService>()
                .AddScoped<IParentService, ParentService>()
                .AddScoped<IStudentService, StudentService>()
                .AddScoped<IAttendanceService, AttendanceService>()
                .AddScoped<IGradeService, GradeService>()
                .AddScoped<IParentInsightsService, ParentInsightsService>()
                .AddScoped<ILessonReportService, LessonReportService>()
                .AddScoped<INotificationService, NotificationService>()
                .AddScoped<IMaterialService, MaterialService>()
                .AddScoped<IAnnouncementService, AnnouncementService>()
                .AddScoped<ICurrentUserService, CurrentUserService>()
                .AddScoped<IFileStorageService, LocalFileStorageService>()
                .AddFirebaseServices(config)
                .AddScoped<IPaymentService, PaymentService>()
                .AddScoped<ISessionPaymentLinker, SessionPaymentLinker>()
                .AddIdentityServices()
                .AddPermissions()
                .AddRateLimitingPolicies()
                .AddOpenApiDocumentation(config)
                .Configure<MailSettings>(config.GetSection("MailSettings"))
                .AddHttpContextAccessor()
                .AddSingleton<IDateTimeService, DateTimeService>();
        }

        public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider,
        CancellationToken ct = default)
        {
            using var scope = serviceProvider.CreateScope();

            await scope.ServiceProvider.GetRequiredService<ITenantDbSeeder>()
                .InitializeDatabaseAsync(ct);
        }

        /// <summary>
        /// Registers Firebase push notifications if credentials are configured,
        /// otherwise falls back to the mock (log-only) implementation.
        /// To enable: set Firebase:CredentialsPath in appsettings to the path of your
        /// Firebase service-account JSON file (downloaded from Firebase Console → Project Settings → Service Accounts).
        /// </summary>
        internal static IServiceCollection AddFirebaseServices(this IServiceCollection services, IConfiguration config)
        {
            var credentialsPath = config["Firebase:CredentialsPath"];
            var hasCredentials = !string.IsNullOrWhiteSpace(credentialsPath) && File.Exists(credentialsPath);

            if (hasCredentials)
            {
                // FirebaseApp is a singleton — initialise only once across the application lifetime
                if (FirebaseApp.DefaultInstance == null)
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(credentialsPath)
                    });
                }

                services.AddScoped<IPushNotificationService, FirebasePushNotificationService>();
            }
            else
            {
                // No credentials → use mock so the rest of the app still works in development
                services.AddScoped<IPushNotificationService, MockPushNotificationService>();
            }

            return services;
        }

        internal static IServiceCollection AddIdentityServices(
            this IServiceCollection services)
        {
            services
                .AddIdentity<ApplicationUser, ApplicationRole>(options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.User.RequireUniqueEmail = true;

                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                    options.Lockout.MaxFailedAccessAttempts = 5; 
                    options.Lockout.AllowedForNewUsers = true; 
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .Services
                .AddScoped<ITokenService, TokenService>();

            return services;
        }
        internal static IServiceCollection AddPermissions(this IServiceCollection services)
        {
            return services
                .AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>()
                .AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        }

        public static JwtSettings GetJwtSettings(this IServiceCollection services, IConfiguration config)
        {
            var jwtSettingsConfig = config.GetSection("JwtSettings");
            services.Configure<JwtSettings>(jwtSettingsConfig);

            var settings = jwtSettingsConfig.Get<JwtSettings>()!;

            if (string.IsNullOrEmpty(settings.Secret) || settings.Secret.Length < 32)
            {
                throw new InvalidOperationException("JWT Secret is too short! It must be at least 32 characters long for SHA256 security.");
            }

            // Issuer/Audience are baked into every token AND required to validate it.
            // If either is missing, tokens get issued without iss/aud and then fail
            // refresh validation (IDX10206) — a silent, confusing break. Fail fast
            // at startup instead so a misconfigured deployment never boots.
            if (string.IsNullOrWhiteSpace(settings.Issuer) || string.IsNullOrWhiteSpace(settings.Audience))
            {
                throw new InvalidOperationException("JWT Issuer and Audience must be configured (JwtSettings:Issuer / JwtSettings:Audience).");
            }

            return settings;
        }


        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, JwtSettings jwtSettings)
        {
            var secret = Encoding.UTF8.GetBytes(jwtSettings.Secret);

            services
                .AddAuthentication(auth =>
                {
                    auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(bearer =>
                {
                    bearer.RequireHttpsMetadata = false;
                    bearer.SaveToken = true;
                    bearer.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(secret),

                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,

                        ClockSkew = TimeSpan.Zero,
                        RoleClaimType = ClaimTypes.Role,
                        ValidateLifetime = true,
                    };
                    bearer.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                            var claimsPrincipal = context.Principal;

                            var userId = claimsPrincipal.FindFirstValue(JwtRegisteredClaimNames.Sub);
                            var tokenSecurityStamp = claimsPrincipal.FindFirstValue("SecurityStamp");

                            if (!string.IsNullOrEmpty(userId))
                            {
                                var user = await userManager.FindByIdAsync(userId);
                                if (user == null || user.SecurityStamp != tokenSecurityStamp)
                                {
                                    context.Fail("Security Token has expired or been invalidated.");
                                }
                            }
                        },

                        OnAuthenticationFailed = context =>
                        {
                            if (!context.Response.HasStarted)
                            {
                                // Any token-validation failure is an auth problem → 401, so the
                                // mobile client kicks off its refresh/login flow (it only reacts to
                                // 401). Returning 500 used to mask invalid tokens as server errors.
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

                                context.Response.ContentType = "application/json";

                                var message = context.Exception is SecurityTokenExpiredException
                                    ? "Token has expired."
                                    : "Invalid authentication token.";

                                var result = Result.Failure(message);

                                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                                var jsonResult = System.Text.Json.JsonSerializer.Serialize(result, options);

                                return context.Response.WriteAsync(jsonResult);
                            }
                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            context.HandleResponse();
                            if (!context.Response.HasStarted)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.Response.ContentType = "application/json";

                                var result = Result.Failure("You are not authorized.");
                                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                                var jsonResult = System.Text.Json.JsonSerializer.Serialize(result, options);

                                return context.Response.WriteAsync(jsonResult);
                            }
                            return Task.CompletedTask;
                        },
                        OnForbidden = context =>
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                            context.Response.ContentType = "application/json";

                            var result = Result.Failure("You are not authorized to access this resource.");
                            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                            var jsonResult = System.Text.Json.JsonSerializer.Serialize(result, options);

                            return context.Response.WriteAsync(jsonResult);
                        }
                    };
                });

            services.AddAuthorization(options =>
            {
                foreach (var type in typeof(AppPermissions)
                    .GetNestedTypes())
                {
                    foreach (var prop in type.GetFields(
                        BindingFlags.Public |
                        BindingFlags.Static |
                        BindingFlags.FlattenHierarchy))
                    {
                        var propertyValue = prop.GetValue(null);
                        if (propertyValue is not null)
                        {
                            options.AddPolicy(
                                propertyValue.ToString()!,
                                policy => policy.RequireClaim(ClaimConstants.Permission, propertyValue.ToString()!));
                        }
                    }
                }
            });

            return services;
        }

        internal static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services, IConfiguration config)
        {
            var swaggerSettings = config
                .GetSection(nameof(SwaggerSettings))
                .Get<SwaggerSettings>()!;

            services.AddEndpointsApiExplorer();

            services.AddOpenApiDocument((document, _) =>
            {
                document.PostProcess = doc =>
                {
                    doc.Info.Title = swaggerSettings.Title;
                    doc.Info.Description = swaggerSettings.Description;
                    doc.Info.Contact = new OpenApiContact
                    {
                        Name = swaggerSettings.ContactName,
                        Email = swaggerSettings.ContactEmail,
                        Url = swaggerSettings.ContactUrl
                    };
                    doc.Info.License = new OpenApiLicense
                    {
                        Name = swaggerSettings.LicenseName,
                        Url = swaggerSettings.LicenseUrl
                    };
                };

                document.AddSecurity(
                    JwtBearerDefaults.AuthenticationScheme,
                    new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Description = "Enter your Bearer Token to attach it as a header on your requests",
                        In = OpenApiSecurityApiKeyLocation.Header,
                        Type = OpenApiSecuritySchemeType.Http,
                        Scheme = JwtBearerDefaults.AuthenticationScheme,
                        BearerFormat = "JWT"
                    });

                document.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor());
                document.OperationProcessors.Add(new SwaggerGlobalAuthProcessor());
                document.OperationProcessors.Add(new SwaggerHeaderAttributeProcessor());
            });

            return services;
        }
        public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app, IHostEnvironment env)
        {
            app.UseAuthentication()
               .UseMultiTenant()
               .UseRateLimiter()
               .UseAuthorization();

            // Swagger / OpenAPI is a development aid — don't publish the full API
            // surface map to the public in production.
            if (env.IsDevelopment())
                app.UseOpenApiDocumentation();

            return app;
        }
        internal static IApplicationBuilder UseOpenApiDocumentation(this IApplicationBuilder app)
        {
            app.UseOpenApi();
            app.UseSwaggerUi(options =>
            {
                options.DefaultModelExpandDepth = -1;
                options.DocExpansion = "none";
                options.TagsSorter = "alpha";
            });

            return app;
        }

        internal static IServiceCollection AddRateLimitingPolicies(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Local load-testing escape hatch: in Development only, setting the
                // FAHEEM_LOADTEST=1 environment variable removes the global limiter so
                // we can measure the app's true capacity from a single machine. The
                // Development check makes this IMPOSSIBLE to trigger in production.
                var loadTestBypass =
                    string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                                  "Development", StringComparison.OrdinalIgnoreCase)
                    && Environment.GetEnvironmentVariable("FAHEEM_LOADTEST") == "1";

                // ── Global limiter (applies to every request) ────────────────────
                // Authenticated traffic is partitioned PER USER (from the JWT 'sub'),
                // so people behind one shared public IP — mobile carrier CGNAT, a
                // school / centre Wi-Fi — each get their own budget instead of fighting
                // over a single IP bucket. Anonymous traffic falls back to per-IP as a
                // flood backstop.
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    if (loadTestBypass)
                        return RateLimitPartition.GetNoLimiter("loadtest");

                    var userId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                              ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                    if (httpContext.User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(userId))
                    {
                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: $"user:{userId}",
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                AutoReplenishment = true,
                                PermitLimit = 240,          // ~4 req/sec sustained per user
                                QueueLimit = 0,
                                Window = TimeSpan.FromMinutes(1)
                            });
                    }

                    var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"ip:{ip}",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 120,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        });
                });

                // ── Anonymous auth endpoints ─────────────────────────────────────
                // Still per-IP, but raised so a whole class signing in / verifying on
                // one shared connection isn't blocked. Per-ACCOUNT brute force is
                // already handled by ASP.NET Identity lockout (AccessFailedAsync on a
                // wrong password locks that specific account).
                options.AddPolicy("OtpPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 20,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                options.AddPolicy("LoginPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 40,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                options.AddPolicy("RegisterPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 15,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        }));
            });

            return services;
        }


    }
}
