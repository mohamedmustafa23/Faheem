using Application.Features.Sessions.Commands;
using Application.Features.Sessions.DTOs;
using Application.Features.Sessions.Queries;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Infrastructure.Identity.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Teacher
{
    [Route("api/teacher/sessions")]
    [Authorize(Roles = $"{RoleConstants.Teacher},{RoleConstants.Assistant}")]
    [OpenApiTag("Teacher - Sessions", Description = "Endpoints for managing recurring session schedules and today's occurrences")]
    public class TeacherSessionsController : BaseApiController
    {
        [HttpPost]
        [ShouldHavePermission(AppAction.Create, AppFeature.Sessions)]
        [OpenApiOperation("Create Schedule(s)", "Creates one or more recurring session schedules for a group and generates the first occurrence for each.")]
        public async Task<IActionResult> CreateSchedulesAsync([FromBody] CreateSessionRequest request)
        {
            var command = new CreateSessionCommand { Request = request, TenantId = User.GetTenant()! };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPut]
        [ShouldHavePermission(AppAction.Update, AppFeature.Sessions)]
        [OpenApiOperation("Update Schedule", "Updates a recurring schedule's day/time. Conflict detection applies across all active schedules.")]
        public async Task<IActionResult> UpdateScheduleAsync([FromBody] UpdateSessionCommand command)
        {
            command.TenantId = User.GetTenant()!;
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        [ShouldHavePermission(AppAction.Delete, AppFeature.Sessions)]
        [OpenApiOperation("Deactivate Schedule", "Deactivates a recurring schedule and cancels all future occurrences.")]
        public async Task<IActionResult> DeactivateScheduleAsync(Guid id)
        {
            var command = new DeleteSessionCommand { SessionId = id, TenantId = User.GetTenant()! };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpGet("today")]
        [ShouldHavePermission(AppAction.Read, AppFeature.Sessions)]
        [OpenApiOperation("Get Today's Schedule", "Gets all session occurrences for a specific date (YYYY-MM-DD). Defaults to today (UTC) if not provided. Pass includePending=true to also return overdue past-dated occurrences still in Scheduled status.")]
        public async Task<IActionResult> GetTodayScheduleAsync([FromQuery] DateOnly? todayDate, [FromQuery] bool includePending = false)
        {
            var query = new GetTodayScheduleQuery
            {
                TenantId       = User.GetTenant()!,
                TodayDate      = todayDate,
                IncludePending = includePending
            };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpPost("occurrences/manual")]
        [ShouldHavePermission(AppAction.Create, AppFeature.Sessions)]
        [OpenApiOperation("Create Manual Session", "Creates a one-off session not tied to a recurring schedule. Set CountsForPayment=false for bonus/gift sessions.")]
        public async Task<IActionResult> CreateManualOccurrenceAsync([FromBody] CreateManualOccurrenceRequest request)
        {
            var command = new CreateManualOccurrenceCommand { Request = request, TenantId = User.GetTenant()! };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPut("occurrences/{occurrenceId}/cancel")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Sessions)]
        [OpenApiOperation("Cancel Occurrence", "Cancels a specific session occurrence without affecting the recurring schedule.")]
        public async Task<IActionResult> CancelOccurrenceAsync(Guid occurrenceId)
        {
            var response = await Sender.Send(new CancelSessionCommand { OccurrenceId = occurrenceId, TenantId = User.GetTenant()! });
            return Ok(response);
        }

        [HttpDelete("occurrences/{occurrenceId}/manual")]
        [ShouldHavePermission(AppAction.Delete, AppFeature.Sessions)]
        [OpenApiOperation("Delete Manual Occurrence", "Permanently deletes a manual (standalone) session occurrence and reverses its payment side-effects. Cannot delete completed occurrences or those with settled payments.")]
        public async Task<IActionResult> DeleteManualOccurrenceAsync(Guid occurrenceId)
        {
            var response = await Sender.Send(new DeleteManualOccurrenceCommand { OccurrenceId = occurrenceId, TenantId = User.GetTenant()! });
            return Ok(response);
        }

        [HttpPut("occurrences/{occurrenceId}/manual")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Sessions)]
        [OpenApiOperation("Update Manual Occurrence", "Updates the date and time of a scheduled manual session occurrence. PaymentMode and price cannot be changed. Conflict detection applies.")]
        public async Task<IActionResult> UpdateManualOccurrenceAsync(Guid occurrenceId, [FromBody] UpdateManualOccurrenceCommand command)
        {
            command.OccurrenceId = occurrenceId;
            command.TenantId     = User.GetTenant()!;
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPut("occurrences/{occurrenceId}/recurring")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Sessions)]
        [OpenApiOperation("Update Recurring Occurrence",
            "Reschedules a single occurrence that came from a recurring schedule — moves just THIS week. The schedule itself stays unchanged, so next week reverts to the original slot. Conflict detection applies.")]
        public async Task<IActionResult> UpdateRecurringOccurrenceAsync(Guid occurrenceId, [FromBody] UpdateRecurringOccurrenceCommand command)
        {
            command.OccurrenceId = occurrenceId;
            command.TenantId     = User.GetTenant()!;
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpDelete("occurrences/{occurrenceId}/recurring")]
        [ShouldHavePermission(AppAction.Delete, AppFeature.Sessions)]
        [OpenApiOperation("Delete Recurring Occurrence",
            "Physically removes a single recurring occurrence. No payment side-effects. If no other future scheduled occurrence exists on the schedule, next week's is auto-generated to keep the chain alive.")]
        public async Task<IActionResult> DeleteRecurringOccurrenceAsync(Guid occurrenceId)
        {
            var response = await Sender.Send(new DeleteRecurringOccurrenceCommand { OccurrenceId = occurrenceId, TenantId = User.GetTenant()! });
            return Ok(response);
        }
    }
}
