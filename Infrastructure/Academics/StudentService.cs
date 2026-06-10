using Application.Features.Students.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Infrastructure.Contexts;
using Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Academics
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly TenantDbContext _tenantDbContext;

        public StudentService(ApplicationDbContext dbContext, TenantDbContext tenantDbContext)
        {
            _dbContext = dbContext;
            _tenantDbContext = tenantDbContext;
        }

        public async Task<List<StudentGroupDto>> GetMyGroupsAsync(string studentId, CancellationToken ct = default)
        {
            var groupStudents = await _dbContext.GroupStudents
                .Include(gs => gs.Group)
                .Where(gs => gs.StudentId == studentId)
                .ToListAsync(ct);

            var tenantIds = groupStudents.Select(gs => gs.TenantId).Distinct().ToList();
            var tenants = await _tenantDbContext.TenantInfo
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

            return groupStudents.Select(gs => new StudentGroupDto
            {
                GroupId = gs.GroupId,
                GroupName = gs.Group.Name,
                Subject = gs.Group.Subject,
                TeacherName = tenants.TryGetValue(gs.TenantId, out var tName) ? tName : "Unknown Teacher"
            }).ToList();
        }

        public async Task<List<StudentTodaySessionDto>> GetMyTodayScheduleAsync(string studentId, DateOnly today, CancellationToken ct = default)
        {
            var myGroupIds = await _dbContext.GroupStudents
                .Where(gs => gs.StudentId == studentId)
                .Select(gs => gs.GroupId)
                .ToListAsync(ct);

            return await _dbContext.SessionOccurrences
                .Include(o => o.Session)
                .Include(o => o.Group)
                .Where(o =>
                    myGroupIds.Contains(o.GroupId) &&
                    o.OccurrenceDate == today)
                .OrderBy(o => o.Session != null ? o.Session.StartTime : o.StartTime)
                .Select(o => new StudentTodaySessionDto
                {
                    OccurrenceId = o.Id,
                    GroupName = o.Group.Name,
                    Subject = o.Group.Subject,
                    StartTime = o.StartTime ?? o.Session!.StartTime,
                    EndTime = o.EndTime ?? o.Session!.EndTime,
                    Status = o.Status.ToString()
                })
                .ToListAsync(ct);
        }

        public async Task<List<PendingParentRequestDto>> GetPendingParentRequestsAsync(string studentId, CancellationToken ct = default)
        {
            return await _dbContext.ParentStudentLinks
                .Include(l => l.Parent)
                .Where(l => l.StudentUserId == studentId && l.Status == LinkStatus.Pending)
                .Select(l => new PendingParentRequestDto
                {
                    LinkId = l.Id,
                    ParentName = $"{l.Parent.FirstName} {l.Parent.LastName}",
                    ParentPhone = l.Parent.PhoneNumber ?? "",
                    RequestedAt = l.RequestedAt
                }).ToListAsync(ct);
        }

        public async Task<List<LinkedParentDto>> GetMyLinkedParentsAsync(string studentId, CancellationToken ct = default)
        {
            return await _dbContext.ParentStudentLinks
                .Include(l => l.Parent)
                .Where(l => l.StudentUserId == studentId && l.Status == LinkStatus.Accepted)
                .OrderByDescending(l => l.ConfirmedAt)
                .Select(l => new LinkedParentDto
                {
                    ParentId    = l.ParentUserId,
                    FullName    = $"{l.Parent.FirstName} {l.Parent.LastName}",
                    PhoneNumber = l.Parent.PhoneNumber,
                    Email       = l.Parent.Email,
                    LinkedSince = l.ConfirmedAt
                }).ToListAsync(ct);
        }
    }
}
