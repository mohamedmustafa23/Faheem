using Application.Exceptions;
using Application.Features.LessonReports.DTOs;
using Application.Features.Notifications.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Academics
{
    // Per-session lesson reports: a group-level summary (what was covered + optional
    // homework) plus per-student feedback the parent reads to follow their child.
    // Data access mirrors ParentInsightsService (plain _db, tenant filter applies);
    // notifications mirror AnnouncementService.
    public class LessonReportService : ILessonReportService
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notificationService;

        public LessonReportService(ApplicationDbContext db, INotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }

        public async Task<LessonReportEditorDto> GetOccurrenceReportAsync(Guid occurrenceId, string tenantId, CancellationToken ct = default)
        {
            var occurrence = await _db.SessionOccurrences
                .Where(o => o.Id == occurrenceId)
                .Select(o => new { o.Id, o.GroupId, GroupName = o.Group.Name, o.OccurrenceDate })
                .FirstOrDefaultAsync(ct);
            if (occurrence == null) throw new NotFoundException(["Session not found."]);

            // Only students who attended — the others weren't there to give feedback on.
            var presentStudents = await (
                from a in _db.AttendanceRecords
                where a.OccurrenceId == occurrenceId && a.Status == AttendanceStatus.Present
                join u in _db.Users on a.StudentId equals u.Id
                orderby u.FirstName
                select new { a.StudentId, Name = (u.FirstName + " " + u.LastName).Trim() }
            ).ToListAsync(ct);

            var report = await _db.LessonReports
                .Include(r => r.Entries)
                .FirstOrDefaultAsync(r => r.OccurrenceId == occurrenceId, ct);

            var entryByStudent = report?.Entries.ToDictionary(e => e.StudentId, e => e)
                ?? new Dictionary<string, LessonReportEntry>();

            return new LessonReportEditorDto
            {
                OccurrenceId = occurrenceId,
                GroupId = occurrence.GroupId,
                GroupName = occurrence.GroupName,
                Date = occurrence.OccurrenceDate,
                HasReport = report != null,
                LessonTopic = report?.LessonTopic,
                Homework = report?.Homework,
                Students = presentStudents.Select(s =>
                {
                    entryByStudent.TryGetValue(s.StudentId, out var e);
                    return new StudentFeedbackDto
                    {
                        StudentId = s.StudentId,
                        StudentName = s.Name,
                        Performance = e?.Performance,
                        Participation = e?.Participation,
                        HomeworkResult = e?.HomeworkResult,
                        Note = e?.Note,
                    };
                }).ToList(),
            };
        }

        public async Task<string> SaveReportAsync(SaveLessonReportRequest request, string tenantId, CancellationToken ct = default)
        {
            var occurrence = await _db.SessionOccurrences
                .FirstOrDefaultAsync(o => o.Id == request.OccurrenceId, ct);
            if (occurrence == null) throw new NotFoundException(["Session not found."]);

            var report = await _db.LessonReports
                .Include(r => r.Entries)
                .FirstOrDefaultAsync(r => r.OccurrenceId == request.OccurrenceId, ct);

            bool isNew = report == null;
            if (isNew)
            {
                report = new LessonReport
                {
                    OccurrenceId = request.OccurrenceId,
                    GroupId = occurrence.GroupId,
                    TenantId = tenantId,
                };
                await _db.LessonReports.AddAsync(report, ct);
            }

            report!.LessonTopic = string.IsNullOrWhiteSpace(request.LessonTopic) ? null : request.LessonTopic.Trim();
            report.Homework = string.IsNullOrWhiteSpace(request.Homework) ? null : request.Homework.Trim();

            // Upsert per-student feedback. FK is fixed up via the navigation collection.
            var existingByStudent = report.Entries.ToDictionary(e => e.StudentId, e => e);
            foreach (var input in request.Entries)
            {
                var note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
                if (existingByStudent.TryGetValue(input.StudentId, out var entry))
                {
                    entry.Performance = input.Performance;
                    entry.Participation = input.Participation;
                    entry.HomeworkResult = input.HomeworkResult;
                    entry.Note = note;
                }
                else
                {
                    report.Entries.Add(new LessonReportEntry
                    {
                        StudentId = input.StudentId,
                        Performance = input.Performance,
                        Participation = input.Participation,
                        HomeworkResult = input.HomeworkResult,
                        Note = note,
                        TenantId = tenantId,
                    });
                }
            }

            await _db.SaveChangesAsync(ct);

            // Notify only on first creation — later edits stay silent so we don't spam.
            if (isNew && request.Entries.Count > 0)
            {
                var studentIds = request.Entries.Select(e => e.StudentId).Distinct().ToList();
                await _notificationService.SendStudentAndParentNotificationsAsync(
                    studentIds,
                    new NotificationPayload(
                        title: "ملخص حصة جديد",
                        message: "أضاف معلمك ملخص الحصة وملاحظاته.",
                        type: NotificationType.Broadcast,
                        route: $"/student/groups/{occurrence.GroupId}?tab=reports"),
                    parentPayloadFactory: (sid, name) => new NotificationPayload(
                        title: "ملخص حصة جديد لابنك",
                        message: $"أضاف المعلم ملخص حصة وملاحظات عن {name}.",
                        type: NotificationType.Broadcast,
                        route: $"/parent/children/{sid}/group/{occurrence.GroupId}"),
                    tenantId,
                    ct);
            }

            return "تم حفظ ملخص الحصة.";
        }

        public async Task<List<ChildLessonReportDto>> GetStudentGroupReportsAsync(string studentId, Guid groupId, CancellationToken ct = default)
        {
            var enrolled = await _db.GroupStudents.AnyAsync(gs => gs.GroupId == groupId && gs.StudentId == studentId, ct);
            if (!enrolled) throw new ForbiddenException(["You are not enrolled in this group."]);

            return await GetChildGroupReportsAsync(studentId, groupId, ct);
        }

        public async Task<List<ChildLessonReportDto>> GetChildGroupReportsAsync(string childId, Guid groupId, CancellationToken ct = default)
        {
            var reports = await (
                from r in _db.LessonReports
                where r.GroupId == groupId
                join o in _db.SessionOccurrences on r.OccurrenceId equals o.Id
                orderby o.OccurrenceDate descending
                select new { r.Id, o.OccurrenceDate, r.LessonTopic, r.Homework }
            ).ToListAsync(ct);
            if (reports.Count == 0) return new();

            var reportIds = reports.Select(r => r.Id).ToList();
            var entries = await _db.LessonReportEntries
                .Where(e => reportIds.Contains(e.LessonReportId) && e.StudentId == childId)
                .ToDictionaryAsync(e => e.LessonReportId, e => e, ct);

            return reports.Select(r =>
            {
                entries.TryGetValue(r.Id, out var e);
                return new ChildLessonReportDto
                {
                    ReportId = r.Id,
                    Date = r.OccurrenceDate,
                    LessonTopic = r.LessonTopic,
                    Homework = r.Homework,
                    Performance = e?.Performance,
                    Participation = e?.Participation,
                    HomeworkResult = e?.HomeworkResult,
                    Note = e?.Note,
                };
            }).ToList();
        }
    }
}
