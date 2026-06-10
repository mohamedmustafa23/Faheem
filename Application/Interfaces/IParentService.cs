using Application.Features.Parents.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IParentService
    {
        Task<List<LinkedChildDto>> GetMyChildrenAsync(string parentId, CancellationToken ct);
        Task<ChildDetailsDto> GetChildDetailsAsync(string parentId, string childId, DateOnly today, CancellationToken ct);

        /// <summary>
        /// Authorization gate used by every parent-child query handler. Returns
        /// true iff there's an Accepted parent-student link.
        /// </summary>
        Task<bool> IsParentLinkedToChildAsync(string parentId, string childId, CancellationToken ct = default);

        /// <summary>
        /// Unified "at-a-glance" snapshot used by both the parent dashboard
        /// child card and the Overview tab inside child details. Aggregates
        /// attendance, grades, payments, and today's schedule into one trip.
        /// </summary>
        Task<ChildOverviewDto> GetChildOverviewAsync(string parentId, string childId, DateOnly today, CancellationToken ct = default);

        /// <summary>
        /// Announcements across every group the child is enrolled in, newest
        /// first, each row tagged with its source group and teacher.
        /// </summary>
        Task<List<ChildAnnouncementDto>> GetChildAnnouncementsAsync(string parentId, string childId, CancellationToken ct = default);

        /// <summary>
        /// Recent absences (and excused absences) for the child across every
        /// group, newest first. Drives the parent's attendance-tab timeline:
        /// each row tells the parent which session was missed, when, in which
        /// group, whether it was excused, and the teacher's note (excuse reason).
        /// </summary>
        Task<List<ChildAbsenceDto>> GetChildAbsencesAsync(string parentId, string childId, int take = 30, CancellationToken ct = default);
    }
}
