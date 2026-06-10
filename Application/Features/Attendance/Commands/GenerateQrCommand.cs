using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Attendance.Commands
{
    public class GenerateQrCommand : IRequest<Result<string>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public Guid OccurrenceId { get; set; }
    }

    public class GenerateQrCommandHandler : IRequestHandler<GenerateQrCommand, Result<string>>
    {
        private readonly IAttendanceService _attendanceService;
        public GenerateQrCommandHandler(IAttendanceService attendanceService) => _attendanceService = attendanceService;

        public async Task<Result<string>> Handle(GenerateQrCommand command, CancellationToken cancellationToken)
        {
            var token = await _attendanceService.GenerateQrTokenAsync(command.OccurrenceId, command.TenantId, cancellationToken);
            return Result<string>.Success(token);
        }
    }
}
