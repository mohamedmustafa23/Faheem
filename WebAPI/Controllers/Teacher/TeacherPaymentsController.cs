using Application.Features.Payments.Commands;
using Application.Features.Payments.Queries;
using Domain.Enums;
using Infrastructure.Constants;
using Infrastructure.Identity;
using Infrastructure.Identity.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace WebAPI.Controllers.Teacher
{
    [Route("api/teacher/payments")]
    [Authorize(Roles = $"{RoleConstants.Teacher},{RoleConstants.Assistant}")]
    [OpenApiTag("Teacher - Payments", Description = "Endpoints for recording and reviewing student payments")]
    public class TeacherPaymentsController : BaseApiController
    {
        // ── Queries ───────────────────────────────────────────────────────────

        [HttpGet("groups/{groupId}/summary")]
        [ShouldHavePermission(AppAction.Read, AppFeature.Payments)]
        [OpenApiOperation("Get Group Financial Summary",
            "Aggregated finances for the group: current cycle stats + all standalone occurrences. Waived records are excluded from expected totals.")]
        public async Task<IActionResult> GetGroupFinancialSummaryAsync([FromRoute] Guid groupId)
        {
            var query = new GetGroupFinancialSummaryQuery
            {
                GroupId  = groupId,
                TenantId = User.GetTenant()!
            };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("overview")]
        [ShouldHavePermission(AppAction.Read, AppFeature.Payments)]
        [OpenApiOperation("Get Teacher Financial Overview",
            "Grand-total finance report across every group, every cycle (open + closed), and every standalone session. Drives the headline finance card.")]
        public async Task<IActionResult> GetTeacherFinancialOverviewAsync()
        {
            var query = new GetTeacherFinancialOverviewQuery
            {
                TenantId = User.GetTenant()!
            };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("groups/{groupId}/cycles")]
        [ShouldHavePermission(AppAction.Read, AppFeature.Payments)]
        [OpenApiOperation("Get Payment Cycles", "All payment cycles for a group (newest first).")]
        public async Task<IActionResult> GetGroupCyclesAsync(
            [FromRoute] Guid groupId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = new GetGroupCyclesQuery { GroupId = groupId, Page = page, PageSize = pageSize };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("cycles/{cycleId}/records")]
        [ShouldHavePermission(AppAction.Read, AppFeature.Payments)]
        [OpenApiOperation("Get Cycle Student Records",
            "Student payment records (with transaction history) for a regular cycle.")]
        public async Task<IActionResult> GetCycleStudentRecordsAsync(
            [FromRoute] Guid cycleId,
            [FromQuery] PaymentStatus? filterStatus,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = new GetCycleStudentRecordsQuery
            {
                CycleId      = cycleId,
                FilterStatus = filterStatus,
                Page         = page,
                PageSize     = pageSize
            };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        [HttpGet("occurrences/{occurrenceId}/records")]
        [ShouldHavePermission(AppAction.Read, AppFeature.Payments)]
        [OpenApiOperation("Get Standalone Occurrence Records",
            "Payment records for a standalone (independent-fee) session occurrence.")]
        public async Task<IActionResult> GetStandaloneOccurrenceRecordsAsync(
            [FromRoute] Guid occurrenceId,
            [FromQuery] PaymentStatus? filterStatus,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = new GetStandaloneOccurrenceRecordsQuery
            {
                OccurrenceId = occurrenceId,
                FilterStatus = filterStatus,
                Page         = page,
                PageSize     = pageSize
            };
            var response = await Sender.Send(query);
            return Ok(response);
        }

        // ── Commands ──────────────────────────────────────────────────────────

        [HttpPost("records/{recordId}/transactions")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Payments)]
        [OpenApiOperation("Record Payment",
            "Adds a payment transaction to a student's record. Supports partial payments — call multiple times for instalments.")]
        public async Task<IActionResult> RecordPaymentAsync(
            [FromRoute] Guid recordId,
            [FromBody] RecordPaymentCommand command)
        {
            command.RecordId = recordId;
            command.TenantId = User.GetTenant()!;
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpDelete("transactions/{transactionId}")]
        [ShouldHavePermission(AppAction.Delete, AppFeature.Payments)]
        [OpenApiOperation("Delete Payment Transaction",
            "Removes a previously-recorded payment transaction. Status is automatically recomputed.")]
        public async Task<IActionResult> DeletePaymentTransactionAsync([FromRoute] Guid transactionId)
        {
            var command = new DeletePaymentTransactionCommand
            {
                TransactionId = transactionId,
                TenantId      = User.GetTenant()!
            };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPost("cycles/{cycleId}/close")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Payments)]
        [OpenApiOperation("Close Cycle Manually",
            "Closes the specified open cycle and immediately opens the next one for all enrolled students.")]
        public async Task<IActionResult> CloseCycleAsync([FromRoute] Guid cycleId)
        {
            var command = new CloseCycleCommand
            {
                CycleId  = cycleId,
                TenantId = User.GetTenant()!
            };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPost("records/{recordId}/waive")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Payments)]
        [OpenApiOperation("Waive Payment Record",
            "Marks a student's obligation as waived (forgiven). Already-paid records cannot be waived.")]
        public async Task<IActionResult> WaivePaymentAsync([FromRoute] Guid recordId)
        {
            var command = new WaivePaymentCommand
            {
                RecordId = recordId,
                TenantId = User.GetTenant()!
            };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPost("records/{recordId}/unwaive")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Payments)]
        [OpenApiOperation("Unwaive Payment Record",
            "Reverses a previous waive. Status is recomputed from existing transactions.")]
        public async Task<IActionResult> UnwaivePaymentAsync([FromRoute] Guid recordId)
        {
            var command = new UnwaivePaymentCommand
            {
                RecordId = recordId,
                TenantId = User.GetTenant()!
            };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPost("records/{recordId}/discount")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Payments)]
        [OpenApiOperation("Apply Discount",
            "Sets (or replaces) a discount on a record. Net expected = ExpectedAmount − amount.")]
        public async Task<IActionResult> ApplyDiscountAsync(
            [FromRoute] Guid recordId,
            [FromBody] ApplyDiscountCommand command)
        {
            command.RecordId = recordId;
            command.TenantId = User.GetTenant()!;
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpDelete("records/{recordId}/discount")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Payments)]
        [OpenApiOperation("Remove Discount",
            "Removes any existing discount on a record and recomputes the status.")]
        public async Task<IActionResult> RemoveDiscountAsync([FromRoute] Guid recordId)
        {
            var command = new RemoveDiscountCommand
            {
                RecordId = recordId,
                TenantId = User.GetTenant()!
            };
            var response = await Sender.Send(command);
            return Ok(response);
        }

        [HttpPost("groups/{groupId}/recalibrate-cycle")]
        [ShouldHavePermission(AppAction.Update, AppFeature.Payments)]
        [OpenApiOperation("Recalibrate Current Cycle",
            "Re-applies the group's MonthlyFee and SessionsPerCycle to the active cycle and all non-waived records.")]
        public async Task<IActionResult> RecalibrateCycleAsync([FromRoute] Guid groupId)
        {
            var command = new RecalibrateCycleCommand
            {
                GroupId  = groupId,
                TenantId = User.GetTenant()!
            };
            var response = await Sender.Send(command);
            return Ok(response);
        }
    }
}
