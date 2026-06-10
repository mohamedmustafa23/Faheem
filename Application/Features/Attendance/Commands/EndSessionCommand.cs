using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Attendance.Commands
{
    public class EndSessionCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public Guid OccurrenceId { get; set; }
    }

    public class EndSessionCommandHandler : IRequestHandler<EndSessionCommand, Result>
    {
        private readonly IAttendanceService _attendanceService;
        public EndSessionCommandHandler(IAttendanceService attendanceService) => _attendanceService = attendanceService;

        public async Task<Result> Handle(EndSessionCommand command, CancellationToken cancellationToken)
        {
            var result = await _attendanceService.EndOccurrenceAsync(command.OccurrenceId, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}
