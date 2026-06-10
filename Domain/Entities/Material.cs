using Domain.Contracts;

namespace Domain.Entities
{
    public class Material : IMustHaveTenant
    {
        public Guid Id { get; set; }

        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;

        /// <summary>Original upload filename (e.g. "lesson-1.pdf"). Nullable for legacy rows.</summary>
        public string? FileName { get; set; }

        /// <summary>File size in bytes (0 or null for legacy rows).</summary>
        public long? FileSize { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public string TenantId { get; set; } = string.Empty;
    }
}