using Application.Exceptions;
using Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;

        private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".png", ".jpg", ".jpeg", ".gif", ".webp",
            ".mp4", ".mp3", ".wav",
            ".zip", ".rar", ".txt"
        };

        public LocalFileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folderName, CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                throw new ConflictException(["No file was provided or the file is empty."]);

            if (file.Length > MaxFileSizeBytes)
                throw new ConflictException([$"File size exceeds the maximum allowed size of {MaxFileSizeBytes / 1024 / 1024} MB."]);

            string originalExtension = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(originalExtension))
                throw new ConflictException([$"File type '{originalExtension}' is not allowed."]);

            // Strip all path components from the original filename to prevent path traversal
            string safeFileName = Path.GetFileNameWithoutExtension(file.FileName);
            safeFileName = string.Concat(safeFileName.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
            if (string.IsNullOrWhiteSpace(safeFileName)) safeFileName = "file";

            string uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}{originalExtension}";

            // Anchor on ContentRootPath (always set) instead of WebRootPath, which is
            // null when the published API has no wwwroot folder — that mismatch made
            // uploaded files 404. Program.cs serves "/uploads" from this same folder.
            string baseFolder = Path.Combine(_env.ContentRootPath, "wwwroot");

            string uploadsFolder = Path.Combine(baseFolder, "uploads", folderName);

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Verify the resolved path is still inside the uploads folder (extra safety)
            string resolvedPath = Path.GetFullPath(filePath);
            string resolvedUploads = Path.GetFullPath(uploadsFolder);
            if (!resolvedPath.StartsWith(resolvedUploads, StringComparison.OrdinalIgnoreCase))
                throw new ConflictException(["Invalid file path detected."]);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream, ct);
            }

            return $"/uploads/{folderName}/{uniqueFileName}";
        }

        public Task DeleteFileAsync(string fileUrl, CancellationToken ct = default)
        {
            // Anchor on ContentRootPath (always set) instead of WebRootPath, which is
            // null when the published API has no wwwroot folder — that mismatch made
            // uploaded files 404. Program.cs serves "/uploads" from this same folder.
            string baseFolder = Path.Combine(_env.ContentRootPath, "wwwroot");

            // Use Path.Combine with the relative segments (cross-platform safe)
            string relativePath = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            string physicalPath = Path.Combine(baseFolder, relativePath);

            // Safety check: make sure we're deleting inside the base folder only
            string resolvedPath = Path.GetFullPath(physicalPath);
            string resolvedBase = Path.GetFullPath(baseFolder);
            if (!resolvedPath.StartsWith(resolvedBase, StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;

            if (File.Exists(physicalPath))
                File.Delete(physicalPath);

            return Task.CompletedTask;
        }
    }
}