using Application.Features.Attendance.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Attendance.Commands
{
    public class ScanQrCommand : IRequest<Result>
    {
        [JsonIgnore] public string StudentId { get; set; } = string.Empty;
        public ScanQrRequest Request { get; set; } = new();
    }

    public class ScanQrCommandHandler : IRequestHandler<ScanQrCommand, Result>
    {
        private readonly IAttendanceService _attendanceService;
        public ScanQrCommandHandler(IAttendanceService attendanceService) => _attendanceService = attendanceService;

        public async Task<Result> Handle(ScanQrCommand command, CancellationToken cancellationToken)
        {
            var result = await _attendanceService.ScanQrCodeAsync(command.Request, command.StudentId, cancellationToken);
            return Result.Success(result);
        }
    }
}