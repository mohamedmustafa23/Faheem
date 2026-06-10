using Application.Features.Attendance.Commands;
using Application.Features.Attendance.DTOs;
using Application.Features.Attendance.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Student
{
    [Route("api/student/attendance")]
    [Authorize(Roles = RoleConstants.Student)]
    [OpenApiTag("Student - Attendance", Description = "Endpoints for student attendance (QR Scan & History)")]
    public class StudentAttendanceController : BaseApiController
    {
        [HttpPost("scan")]
        [OpenApiOperation("Scan QR Code", "Records student attendance using the scanned QR code token.")]
        public async Task<IActionResult> ScanQrCodeAsync([FromBody] ScanQrRequest request)
        {
            var command = new ScanQrCommand { Request = request, StudentId = User.GetUserId()! };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpGet("my")]
        [OpenApiOperation("Get My Attendance Summary", "Returns a summary of attendance (present/absent/excused counts and rate) for each group the student is enrolled in.")]
        public async Task<IActionResult> GetMyAttendanceSummaryAsync()
        {
            var query = new GetMyAttendanceSummaryQuery { StudentId = User.GetUserId()! };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("my/groups/{groupId}")]
        [OpenApiOperation("Get My Group Attendance Detail", "Returns the full attendance history for the student in a specific group, including status and QR scan flag per session.")]
        public async Task<IActionResult> GetMyGroupAttendanceDetailAsync(Guid groupId)
        {
            var query = new GetMyGroupAttendanceDetailQuery { StudentId = User.GetUserId()!, GroupId = groupId };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("my/streak")]
        [OpenApiOperation("Get My Attendance Streak", "Returns the count of the student's most recent consecutive Present records across all groups. Stops at the first Absent or Excused.")]
        public async Task<IActionResult> GetMyAttendanceStreakAsync()
        {
            var query = new GetMyAttendanceStreakQuery { StudentId = User.GetUserId()! };
            var response = await Sender.Send(query);
            return Ok(response);
        }
    }
}