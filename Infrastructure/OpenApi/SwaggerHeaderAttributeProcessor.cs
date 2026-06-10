using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using System.Reflection;

namespace Infrastructure.OpenApi
{
    public class SwaggerHeaderAttributeProcessor : IOperationProcessor
    {
        public bool Process(OperationProcessorContext context)
        {
            var swaggerHeader = context.MethodInfo
                .GetCustomAttribute(typeof(SwaggerHeaderAttribute), true)
                as SwaggerHeaderAttribute;

            if (swaggerHeader is null) return true;

            var parameters = context.OperationDescription
                .Operation.Parameters;

            var existingParam = parameters.FirstOrDefault(p =>
                p.Kind == OpenApiParameterKind.Header &&
                p.Name == swaggerHeader.HeaderName);

            if (existingParam is not null)
                parameters.Remove(existingParam);

            parameters.Add(new OpenApiParameter
            {
                Name = swaggerHeader.HeaderName,
                Kind = OpenApiParameterKind.Header,
                Description = swaggerHeader.Description,
                IsRequired = swaggerHeader.IsRequired,
                Schema = new NJsonSchema.JsonSchema
                {
                    Type = NJsonSchema.JsonObjectType.String,
                    Default = swaggerHeader.DefaultValue
                }
            });

            return true;
        }
    }
}
