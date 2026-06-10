using Application.Features.Grades.Commands;
using Application.Features.Grades.DTOs;
using Application.Features.Grades.Queries;
using Application.Wrappers;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Infrastructure.Identity.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Teacher
{
    [Route("api/teacher/grades")]
    [Authorize(Roles = $"{RoleConstants.Teacher},{RoleConstants.Assistant}")]
    [OpenApiTag("Teacher - Grades", Description = "Endpoints for managing exams and student grades")]
    public class TeacherGradesController : BaseApiController
    {
        [HttpPost("exams")]
        [ShouldHavePermission(AppAction.Create, AppFeature.Grades)]
        [OpenApiOperation("Create Exam", "Creates a new exam definition for a group.")]
        public async Task<IActionResult> CreateExamAsync([FromBody] CreateExamRequest request)
        {
            var command = new CreateExamCommand { Request = request, TenantId = User.GetTenant()! };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPut("exams/save-scores")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Grades)]
        [OpenApiOperation("Save/Update Grades", "Saves or updates scores for specific students. Can be called multiple times for partial grading.")]
        public async Task<IActionResult> SaveGradesAsync([FromBody] SaveGradesRequest request)
        {
            var command = new SaveGradesCommand { Request = request, TenantId = User.GetTenant()! };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpGet("groups/{groupId}/exams")]
        [ShouldHavePermission(AppAction.Read, AppFeature.Grades)]
        [OpenApiOperation("Get Group Exams", "Gets all exams created by the teacher for a specific group.")]
        public async Task<IActionResult> GetGroupExamsAsync(Guid groupId)
        {
            var query = new GetGroupExamsQuery { GroupId = groupId, TenantId = User.GetTenant()! };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("exams/{examId}/scores")]
        [ShouldHavePermission(AppAction.Read, AppFeature.Grades)]
        [OpenApiOperation("Get Exam Scores", "Gets all students in the group and their scores for a specific exam.")]
        public async Task<IActionResult> GetExamScoresAsync(Guid examId)
        {
            var query = new GetExamScoresQuery { ExamId = examId, TenantId = User.GetTenant()! };
            var response = await Sender.Send(query);
            return Ok(response);
        }
    }
}