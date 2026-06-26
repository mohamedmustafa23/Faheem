using Domain.Enums;
using Finbuckle.MultiTenant.Abstractions;

namespace Infrastructure.Tenancy
{
    public class AppTenantInfo : ITenantInfo
    {
        public string Id { get; set; }
        public string Identifier { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? ConnectionString { get; set; }
        public DateTime ValidUpTo { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Whether this workspace is a single teacher (Individual) or a center.</summary>
        public TenantType Type { get; set; } = TenantType.Individual;

        /// <summary>
        /// Seat limit for a center package (max active member teachers). Null = unlimited /
        /// not applicable (Individual workspaces). Enforced on invite in the center flow.
        /// </summary>
        public int? MaxTeachers { get; set; }
    }
}
