using Application.Exceptions;
using Application.Features.Materials.DTOs;
using Application.Features.Notifications.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Academics
{
    public class MaterialService : IMaterialService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IFileStorageService _fileStorageService; 
        private readonly INotificationService _notificationService;

        public MaterialService(ApplicationDbContext dbContext, IFileStorageService fileStorageService, INotificationService notificationService)
        {
            _dbContext = dbContext;
            _fileStorageService = fileStorageService;
            _notificationService = notificationService;
        }

        public async Task<string> UploadMaterialAsync(UploadMaterialRequest request, string tenantId, CancellationToken ct = default)
        {
            if (request.GroupIds == null || !request.GroupIds.Any())
                throw new FluentValidation.ValidationException("Group IDs cannot be null or empty.");

            Guid[] cleanGroupIdsArray = request.GroupIds.ToArray();

            string fileUrl = await _fileStorageService.UploadFileAsync(request.File, "materials", ct);

            // Capture filename + size up-front so the UI can render rich rows
            // without re-fetching the file. Both fields are nullable so legacy
            // rows uploaded before this change keep working.
            var materialsToSave = cleanGroupIdsArray.Select(groupId => new Material
            {
                GroupId  = groupId,
                Title    = request.Title,
                FileUrl  = fileUrl,
                FileName = request.File.FileName,
                FileSize = request.File.Length,
                TenantId = tenantId
            }).ToList();

            await _dbContext.Materials.AddRangeAsync(materialsToSave, ct);
            await _dbContext.SaveChangesAsync(ct);

            var studentIds = await _dbContext.GroupStudents
                .Where(gs => cleanGroupIdsArray.Contains(gs.GroupId))
                .Select(gs => gs.StudentId)
                .Distinct()
                .ToListAsync(ct);

            if (studentIds.Any())
            {
                await _notificationService.SendStudentAndParentNotificationsAsync(
                    studentIds,
                    new NotificationPayload(
                        title: "ملف جديد",
                        message: $"تم رفع ملف جديد: {request.Title}.",
                        type: Domain.Enums.NotificationType.Broadcast,
                        route: "/student/materials"),
                    parentPayloadFactory: (sid, name) => new NotificationPayload(
                        title: "ملف جديد لابنك",
                        message: $"تم رفع ملف جديد لـ {name}: {request.Title}.",
                        type: Domain.Enums.NotificationType.Broadcast,
                        route: $"/parent/children/{sid}"),
                    tenantId,
                    ct);
            }

            return "Material uploaded successfully to selected groups.";
        }

        public async Task<List<MaterialResponseDto>> GetGroupMaterialsAsync(Guid groupId, string userId, CancellationToken ct = default)
        {
            var isEnrolled = await _dbContext.GroupStudents
                .AnyAsync(gs => gs.GroupId == groupId && gs.StudentId == userId, ct);

            if (!isEnrolled) throw new ForbiddenException(["You are not enrolled in this group."]);

            return await _dbContext.Materials
                .Where(m => m.GroupId == groupId)
                .OrderByDescending(m => m.UploadedAt)
                .Select(m => new MaterialResponseDto
                {
                    Id         = m.Id,
                    Title      = m.Title,
                    FileUrl    = m.FileUrl,
                    FileName   = m.FileName,
                    FileSize   = m.FileSize,
                    UploadedAt = m.UploadedAt
                }).ToListAsync(ct);
        }

        public async Task<List<MaterialResponseDto>> GetStudentAllMaterialsAsync(string studentId, CancellationToken ct = default)
        {
            // Pull every material across the student's enrolled groups, newest first.
            // Joined to Group so the UI can label which group each result came from
            // — essential context for a cross-group search results screen.
            var groupIds = await _dbContext.GroupStudents
                .Where(gs => gs.StudentId == studentId)
                .Select(gs => gs.GroupId)
                .ToListAsync(ct);

            if (groupIds.Count == 0) return [];

            return await _dbContext.Materials
                .Where(m => groupIds.Contains(m.GroupId))
                .OrderByDescending(m => m.UploadedAt)
                .Select(m => new MaterialResponseDto
                {
                    Id         = m.Id,
                    Title      = m.Title,
                    FileUrl    = m.FileUrl,
                    FileName   = m.FileName,
                    FileSize   = m.FileSize,
                    UploadedAt = m.UploadedAt,
                    GroupId    = m.GroupId,
                    GroupName  = m.Group.Name
                }).ToListAsync(ct);
        }

        public async Task<List<MaterialResponseDto>> GetTeacherMaterialsAsync(Guid groupId, string tenantId, CancellationToken ct = default)
        {
            return await _dbContext.Materials
                .Where(m => m.GroupId == groupId)
                .OrderByDescending(m => m.UploadedAt)
                .Select(m => new MaterialResponseDto
                {
                    Id         = m.Id,
                    Title      = m.Title,
                    FileUrl    = m.FileUrl,
                    FileName   = m.FileName,
                    FileSize   = m.FileSize,
                    UploadedAt = m.UploadedAt
                }).ToListAsync(ct);
        }

        public async Task<string> DeleteMaterialAsync(Guid materialId, string tenantId, CancellationToken ct = default)
        {
            var material = await _dbContext.Materials
                .FirstOrDefaultAsync(m => m.Id == materialId, ct);

            if (material == null) throw new NotFoundException(["Material not found."]);

            bool isFileUsedElsewhere = await _dbContext.Materials
                .AnyAsync(m => m.FileUrl == material.FileUrl && m.Id != materialId, ct);

            if (!isFileUsedElsewhere)
            {
                await _fileStorageService.DeleteFileAsync(material.FileUrl, ct);
            }

            _dbContext.Materials.Remove(material);
            await _dbContext.SaveChangesAsync(ct);

            return "Material deleted successfully.";
        }
    }
}