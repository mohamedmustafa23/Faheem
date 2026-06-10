namespace Infrastructure.OpenApi
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SwaggerHeaderAttribute(
        string headerName,
        string description,
        string? defaultValue,
        bool isRequired) : Attribute
    {
        public string HeaderName { get; } = headerName;
        public string Description { get; } = description;
        public string? DefaultValue { get; } = defaultValue;
        public bool IsRequired { get; } = isRequired;
    }
  
}