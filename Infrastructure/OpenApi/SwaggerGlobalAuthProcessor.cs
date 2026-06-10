using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using System.Reflection;

namespace Infrastructure.OpenApi
{
    public class SwaggerGlobalAuthProcessor : IOperationProcessor
    {
        private readonly string _scheme;

        public SwaggerGlobalAuthProcessor(string scheme)
            => _scheme = scheme;

        public SwaggerGlobalAuthProcessor()
            : this(JwtBearerDefaults.AuthenticationScheme) { }

        public bool Process(OperationProcessorContext context)
        {
            var list = context.OperationDescription
                .Operation
                .TryGetPropertyValue<IList<object>>("tags");

            if (list is not null)
            {
                if (list.OfType<AllowAnonymousAttribute>().Any())
                    return true;
            }

            if (context.OperationDescription
                .Operation.Security?.Count == 0)
            {
                context.OperationDescription.Operation.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        [_scheme] = Array.Empty<string>()
                    }
                ];
            }

            return true;
        }
    }

    // ── Object Extensions ─────────────────────────────────
    public static class ObjectExtensions
    {
        /// <summary>
        /// Tries to get the value of a property by name using reflection.
        /// Returns default(T) if the property does not exist.
        /// </summary>
        public static T? TryGetPropertyValue<T>(
            this object obj, string propertyName)
        {
            var prop = obj.GetType()
                .GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.Instance);

            if (prop is null) return default;
            return (T?)prop.GetValue(obj);
        }
    }
}
