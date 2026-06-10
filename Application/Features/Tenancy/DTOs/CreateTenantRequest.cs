namespace Application.Features.Tenancy.DTOs
{
    public class CreateTenantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ConnectionString { get; set; }
        public DateTime ValidUpTo { get; set; }
    }
}
