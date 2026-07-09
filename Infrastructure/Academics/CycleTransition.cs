using Domain.Entities;
using Domain.Enums;
using Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Academics
{
    /// <summary>
    /// Shared cycle-close logic used by BOTH the manual close (PaymentService) and
    /// the automatic close when a group hits its session target (AttendanceService),
    /// so the two paths can never diverge.
    ///
    /// It closes the given cycle and opens a fresh one with a new unpaid record per
    /// enrolled student. Unpaid balances are NOT moved — each cycle stays its own
    /// bill, and a student's total outstanding is aggregated live across cycles
    /// wherever it's shown (teacher + student + parent). That keeps history accurate
    /// and always reflects later edits to any cycle.
    ///
    /// Modifies tracked entities only; the caller owns the transaction + SaveChanges.
    /// </summary>
    internal static class CycleTransition
    {
        public static async Task<PaymentCycle> CloseAndOpenNextAsync(
            ApplicationDbContext db, PaymentCycle cycle, string tenantId, CancellationToken ct = default)
        {
            cycle.IsCompleted = true;
            cycle.ClosedAt    = DateTime.UtcNow;

            var group = await db.Groups.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == cycle.GroupId, ct);
            int nextTarget  = group?.SessionsPerCycle ?? cycle.SessionsTarget;
            decimal nextFee = group?.MonthlyFee ?? 0;

            // Highest existing number defends against a race between two closes.
            int nextNumber = (await db.PaymentCycles
                .Where(c => c.GroupId == cycle.GroupId)
                .MaxAsync(c => (int?)c.CycleNumber, ct) ?? 0) + 1;

            var newCycle = new PaymentCycle
            {
                GroupId        = cycle.GroupId,
                CycleNumber    = nextNumber,
                SessionsTarget = nextTarget,
                BaseFee        = nextFee,
                TenantId       = tenantId
            };
            db.PaymentCycles.Add(newCycle);

            var enrolledIds = await db.GroupStudents
                .Where(gs => gs.GroupId == cycle.GroupId)
                .Select(gs => gs.StudentId)
                .ToListAsync(ct);

            foreach (var sid in enrolledIds)
            {
                db.StudentPaymentRecords.Add(new StudentPaymentRecord
                {
                    StudentId         = sid,
                    GroupId           = cycle.GroupId,
                    PaymentCycle      = newCycle,
                    EnrolledAtSession = 0,
                    ExpectedAmount    = nextFee,
                    Status            = PaymentStatus.Unpaid,
                    TenantId          = tenantId
                });
            }

            return newCycle;
        }
    }
}
