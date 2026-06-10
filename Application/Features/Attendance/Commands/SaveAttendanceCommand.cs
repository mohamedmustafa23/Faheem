using Application.Features.Attendance.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Attendance.Commands
{
    public class SaveAttendanceCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public SaveAttendanceRequest Request { get; set; } = new();
    }

    public class SaveAttendanceCommandHandler : IRequestHandler<SaveAttendanceCommand, Result>
    {
        private readonly IAttendanceService _attendanceService;
        public SaveAttendanceCommandHandler(IAttendanceService attendanceService) => _attendanceService = attendanceService;

        public async Task<Result> Handle(SaveAttendanceCommand command, CancellationToken cancellationToken)
        {
            var result = await _attendanceService.SaveAttendanceAsync(command.Request, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}