namespace Application.Features.Materials.DTOs
{
    public class MaterialResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public DateTime UploadedAt { get; set; }

        // Group context — needed by the student-facing "all materials" screen
        // so each result can show which group it came from. Nullable so the
        // per-group endpoint can keep returning lean payloads.
        public Guid? GroupId { get; set; }
        public string? GroupName { get; set; }
    }
}