using Application;
using Infrastructure;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using WebApi;
using Microsoft.AspNetCore.Rewrite;
using Sentry.AspNetCore;

namespace WebAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Sentry — captures unexpected server errors (incl. the 500s the error
            // middleware logs) with full stack traces. DSN + options read from the
            // "Sentry" config section. Events are auto-tagged by environment.
            builder.WebHost.UseSentry();

            // Add services to the container.

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                    options.JsonSerializerOptions.Converters.Add(
                        new System.Text.Json.Serialization.JsonStringEnumConverter()));
            builder.Services.AddHealthChecks();


            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:3000", "http://localhost:5173"];

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowConfigured", policy =>
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials());

                // Only used in Development via environment check below
                options.AddPolicy("AllowAll", policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
            });
 
            builder.Services.AddApplicationServices();

            builder.Services.AddInfrastructureServices(builder.Configuration);

            builder.Services.AddJwtAuthentication(builder.Services.GetJwtSettings(builder.Configuration));

            var app = builder.Build();

            // Database Seeder
            await app.Services.InitializeDatabaseAsync();

            // Configure the HTTP request pipeline.
            app.UseHttpsRedirection();
            app.UseCors("AllowConfigured");
            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseInfrastructure(app.Environment);

            // Serve the marketing site (index.html / privacy.html) + runtime-uploaded
            // files (materials, etc.) from ContentRoot/wwwroot. Pointing the provider
            // explicitly avoids a 404 on every upload when the published API shipped
            // without a wwwroot folder (WebRootPath would be null).
            var webRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
            Directory.CreateDirectory(webRoot);
            var webRootProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot);

            // Clean URLs for the marketing pages: "/privacy" and "/delete-account"
            // serve their .html files (no extension needed). Targeted rules only, so
            // API routes and uploads are untouched. Must run before the static files.
            app.UseRewriter(new RewriteOptions()
                .AddRewrite(@"^privacy/?$", "privacy.html", skipRemainingRules: true)
                .AddRewrite(@"^delete-account/?$", "delete-account.html", skipRemainingRules: true));

            // "/" → index.html (the jokolearn.com landing page).
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = webRootProvider });
            app.UseStaticFiles(new StaticFileOptions { FileProvider = webRootProvider });
            app.MapControllers();
            app.MapHealthChecks("/health");

            app.Run();
        }
    }
}
