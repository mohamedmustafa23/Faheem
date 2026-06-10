using Application.Exceptions;
using Application.Features.Grades.DTOs;
using Application.Features.Notifications.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Contexts;
using Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Academics
{
    public class GradeService : IGradeService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly TenantDbContext _tenantDbContext;
        private readonly INotificationService _notificationService;

        public GradeService(ApplicationDbContext dbContext, TenantDbContext tenantDbContext, INotificationService notificationService)
        {
            _dbContext = dbContext;
            _tenantDbContext = tenantDbContext;
            _notificationService = notificationService;
        }

        public async Task<Guid> CreateExamAsync(CreateExamRequest request, string tenantId, CancellationToken ct = default)
        {
            var group = await _dbContext.Groups
                .FirstOrDefaultAsync(g => g.Id == request.GroupId, ct);

            if (group == null)
                throw new NotFoundException(["Group not found."]);

            var exam = new Exam
            {
                GroupId = request.GroupId,
                Name = request.ExamName,
                Date = request.ExamDate,
                MaxScore = request.MaxScore,
                TenantId = tenantId
            };

            await _dbContext.Exams.AddAsync(exam, ct);
            await _dbContext.SaveChangesAsync(ct);

            return exam.Id;
        }

        public async Task<string> SaveGradesAsync(SaveGradesRequest request, string tenantId, CancellationToken ct = default)
        {
            var exam = await _dbContext.Exams
                .FirstOrDefaultAsync(e => e.Id == request.ExamId, ct);

            if (exam == null)
                throw new NotFoundException(["Exam not found."]);

            var invalidScores = request.StudentScores.Where(s => s.Score > exam.MaxScore).ToList();
            if (invalidScores.Any())
                throw new ConflictException([$"One or more scores exceed the exam's maximum score of {exam.MaxScore}."]);

            var enrolledStudentIds = await _dbContext.GroupStudents
                .Where(gs => gs.GroupId == exam.GroupId)
                .Select(gs => gs.StudentId)
                .ToListAsync(ct);

            var invalidStudents = request.StudentScores.Select(s => s.StudentId).Except(enrolledStudentIds).ToList();
            if (invalidStudents.Any())
                throw new ConflictException(["Some students are not enrolled in this group."]);

            var existingGrades = await _dbContext.StudentGrades
                .Where(sg => sg.ExamId == request.ExamId)
                .ToDictionaryAsync(sg => sg.StudentId, sg => sg, ct);

            var newGrades = new List<StudentGrade>();

            foreach (var input in request.StudentScores)
            {
                if (existingGrades.TryGetValue(input.StudentId, out var existingGrade))
                {
                    existingGrade.Score = input.Score;
                }
                else
                {
                    newGrades.Add(new StudentGrade
                    {
                        ExamId = request.ExamId,
                        StudentId = input.StudentId,
                        Score = input.Score,
                        TenantId = tenantId
                    });
                }
            }

            if (newGrades.Any())
                await _dbContext.StudentGrades.AddRangeAsync(newGrades, ct);

            await _dbContext.SaveChangesAsync(ct);


            var studentIds = request.StudentScores.Select(s => s.StudentId).ToList();
            await _notificationService.SendStudentAndParentNotificationsAsync(
                studentIds,
                new NotificationPayload(
                    title: "نتيجة امتحان جديدة",
                    message: $"تم تسجيل نتيجتك في امتحان: {exam.Name}.",
                    type: Domain.Enums.NotificationType.GradePublished,
                    route: "/student/grades"),
                parentPayloadFactory: (sid, name) => new NotificationPayload(
                    title: "نتيجة امتحان لابنك",
                    message: $"تم تسجيل نتيجة {name} في امتحان: {exam.Name}.",
                    type: Domain.Enums.NotificationType.GradePublished,
                    route: $"/parent/children/{sid}"),
                tenantId,
                ct);

            return "Grades saved successfully.";
        }

        public async Task<List<StudentGradeResponseDto>> GetStudentGradesAsync(string studentId, CancellationToken ct = default)
        {
            var grades = await _dbContext.StudentGrades
                .Include(sg => sg.Exam)
                .ThenInclude(e => e.Group)
                .Where(sg => sg.StudentId == studentId)
                .OrderByDescending(sg => sg.Exam.Date)
                .ToListAsync(ct);

            var tenantIds = grades.Select(g => g.TenantId).Distinct().ToList();
            var tenants = await _tenantDbContext.TenantInfo
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

            return grades.Select(sg => new StudentGradeResponseDto
            {
                ExamId      = sg.ExamId,
                ExamName    = sg.Exam.Name,
                Subject     = sg.Exam.Group.Subject,
                ExamDate    = sg.Exam.Date,
                Score       = sg.Score,
                MaxScore    = sg.Exam.MaxScore,
                TeacherName = tenants.TryGetValue(sg.TenantId, out var tName) ? tName : "Unknown Teacher",
                GroupId     = sg.Exam.GroupId,
                GroupName   = sg.Exam.Group.Name
            }).ToList();
        }

        public async Task<List<GroupExamResponseDto>> GetGroupExamsForTeacherAsync(Guid groupId, string tenantId, CancellationToken ct = default)
        {
            return await _dbContext.Exams
                .Where(e => e.GroupId == groupId)
                .OrderByDescending(e => e.Date)
                .Select(e => new GroupExamResponseDto
                {
                    ExamId = e.Id,
                    ExamName = e.Name,
                    ExamDate = e.Date,
                    MaxScore = e.MaxScore
                }).ToListAsync(ct);
        }

        public async Task<List<ExamScoreResponseDto>> GetExamGradesForTeacherAsync(Guid examId, string tenantId, CancellationToken ct = default)
        {
            var exam = await _dbContext.Exams
                .FirstOrDefaultAsync(e => e.Id == examId, ct);

            if (exam == null) throw new NotFoundException(["Exam not found."]);

            var query = from gs in _dbContext.GroupStudents
                        where gs.GroupId == exam.GroupId

                        join u in _dbContext.Users on gs.StudentId equals u.Id

                        join sg in _dbContext.StudentGrades
                             on new { gs.StudentId, ExamId = examId } equals new { sg.StudentId, sg.ExamId } into sgGroup
                        from sg in sgGroup.DefaultIfEmpty() 
                        select new ExamScoreResponseDto
                        {
                            StudentId = gs.StudentId,
                            StudentName = u.FirstName + " " + u.LastName,
                            Score = sg != null ? sg.Score : null 
                        };

            var result = await query.OrderBy(x => x.StudentName).ToListAsync(ct);

            return result;
        }
    }
}