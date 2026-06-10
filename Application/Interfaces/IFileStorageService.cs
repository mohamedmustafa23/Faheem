using Microsoft.AspNetCore.Http;

namespace Application.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string folderName, CancellationToken ct = default);

        Task DeleteFileAsync(string fileUrl, CancellationToken ct = default);
    }
}