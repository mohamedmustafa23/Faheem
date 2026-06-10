using Application.Exceptions;
using Application.Features.Announcements.DTOs;
using Application.Features.Notifications.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Constants;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly INotificationService _notificationService;

        public AnnouncementService(ApplicationDbContext dbContext, INotificationService notificationService)
        {
            _dbContext = dbContext;
            _notificationService = notificationService;
        }

        public async Task<string> CreateAnnouncementAsync(CreateAnnouncementRequest request, string tenantId, CancellationToken ct = default)
        {
            if (request.GroupIds == null || !request.GroupIds.Any())
                throw new FluentValidation.ValidationException("Group IDs cannot be null or empty.");

            Guid[] cleanGroupIdsArray = request.GroupIds.ToArray();

            var announcements = cleanGroupIdsArray.Select(groupId => new GroupAnnouncement
            {
                GroupId = groupId,
                Message = request.Message,
                IsPinned = request.IsPinned,
                TenantId = tenantId
            }).ToList();

            await _dbContext.GroupAnnouncements.AddRangeAsync(announcements, ct);
            await _dbContext.SaveChangesAsync(ct);

            var studentIds = await _dbContext.GroupStudents
                .Where(gs => cleanGroupIdsArray.Contains(gs.GroupId))
                .Select(gs => gs.StudentId)
                .Distinct()
                .ToListAsync(ct);

            if (studentIds.Any())
            {
                // Students get a generic message — a single student may be in
                // multiple addressed groups, so we can't deep-link to one group.
                // Parents see the child's name + a tap into that child's screen.
                await _notificationService.SendStudentAndParentNotificationsAsync(
                    studentIds,
                    new NotificationPayload(
                        title: "إعلان جديد",
                        message: "نشر معلمك إعلانًا جديدًا في مجموعتك.",
                        type: Domain.Enums.NotificationType.Broadcast,
                        route: null),
                    parentPayloadFactory: (sid, name) => new NotificationPayload(
                        title: "إعلان جديد لابنك",
                        message: $"نشر المعلم إعلانًا جديدًا في مجموعة {name}.",
                        type: Domain.Enums.NotificationType.Broadcast,
                        route: $"/parent/children/{sid}"),
                    tenantId,
                    ct);
            }

            return "Announcements posted successfully.";
        }

        public async Task<string> DeleteAnnouncementAsync(Guid announcementId, string tenantId, CancellationToken ct = default)
        {
            var announcement = await _dbContext.GroupAnnouncements
                .FirstOrDefaultAsync(a => a.Id == announcementId, ct); 

            if (announcement == null) throw new NotFoundException(["Announcement not found."]);

            _dbContext.GroupAnnouncements.Remove(announcement);
            await _dbContext.SaveChangesAsync(ct);
            return "Announcement deleted successfully.";
        }

        public async Task<string> TogglePinAsync(Guid announcementId, string tenantId, CancellationToken ct = default)
        {
            var announcement = await _dbContext.GroupAnnouncements
                .FirstOrDefaultAsync(a => a.Id == announcementId, ct); 

            if (announcement == null) throw new NotFoundException(["Announcement not found."]);

            announcement.IsPinned = !announcement.IsPinned;
            await _dbContext.SaveChangesAsync(ct);
            return announcement.IsPinned ? "Announcement pinned." : "Announcement unpinned.";
        }

        public async Task<List<AnnouncementResponseDto>> GetGroupAnnouncementsAsync(Guid groupId, string userId, string userRole, CancellationToken ct = default)
        {
            if (userRole == RoleConstants.Student)
            {
                var isEnrolled = await _dbContext.GroupStudents
                    .AnyAsync(gs => gs.GroupId == groupId && gs.StudentId == userId, ct);
                if (!isEnrolled) throw new ForbiddenException(["You are not enrolled in this group."]);
            }
            else if (userRole == RoleConstants.Parent)
            {
                var childInGroup = await _dbContext.ParentStudentLinks
                    .AnyAsync(p => p.ParentUserId == userId
                                && p.Status == Domain.Enums.LinkStatus.Accepted
                                && _dbContext.GroupStudents.Any(gs => gs.GroupId == groupId && gs.StudentId == p.StudentUserId), ct);

                if (!childInGroup) throw new ForbiddenException(["You do not have access to this group's announcements."]);
            }

            return await _dbContext.GroupAnnouncements
                .Where(a => a.GroupId == groupId)
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.CreatedAt)
                .Take(50)
                .Select(a => new AnnouncementResponseDto
                {
                    Id = a.Id,
                    Message = a.Message,
                    IsPinned = a.IsPinned,
                    CreatedAt = a.CreatedAt
                }).ToListAsync(ct);
        }
    }
}