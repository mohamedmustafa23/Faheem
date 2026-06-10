using Application.Features.Students.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IStudentService
    {
        Task<List<StudentGroupDto>> GetMyGroupsAsync(string studenId, CancellationToken ct);
        Task<List<StudentTodaySessionDto>> GetMyTodayScheduleAsync(string studentId, DateOnly today, CancellationToken ct);
        Task<List<PendingParentRequestDto>> GetPendingParentRequestsAsync(string studentId, CancellationToken ct);

        /// <summary>
        /// Lists every accepted parent link this student has — drives the
        /// "أهلي" panel where they can see who can monitor them and
        /// disconnect anyone they didn't intend to keep.
        /// </summary>
        Task<List<LinkedParentDto>> GetMyLinkedParentsAsync(string studentId, CancellationToken ct = default);
    }
}
