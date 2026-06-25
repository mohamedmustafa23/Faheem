using Application.Features.Parents.DTOs;

namespace Application.Interfaces
{
    public interface IParentInsightsService
    {
        /// <summary>
        /// The group-centric overview for one child: per group, the child's standing
        /// (rank among the group), attendance, grades, and fees. Powers the parent's
        /// redesigned child screen.
        /// </summary>
        Task<List<ChildGroupOverviewDto>> GetChildGroupsOverviewAsync(string childId, CancellationToken ct = default);
    }
}
