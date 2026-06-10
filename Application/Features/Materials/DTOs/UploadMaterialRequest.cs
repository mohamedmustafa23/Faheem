using Microsoft.AspNetCore.Http;

namespace Application.Features.Materials.DTOs
{
    public class UploadMaterialRequest
    {
        public List<Guid> GroupIds { get; set; } = new();
        public string Title { get; set; } = string.Empty;
        public IFormFile File { get; set; } = null!;
    }
}