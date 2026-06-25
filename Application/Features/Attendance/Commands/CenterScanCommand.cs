using Application.Features.Attendance.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Attendance.Commands
{
    public class CenterScanCommand : IRequest<Result<CenterScanResultDto>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public CenterScanRequest Request { get; set; } = new();
    }

    public class CenterScanCommandHandler : IRequestHandler<CenterScanCommand, Result<CenterScanResultDto>>
    {
        private readonly IAttendanceService _attendanceService;
        public CenterScanCommandHandler(IAttendanceService attendanceService) => _attendanceService = attendanceService;

        public async Task<Result<CenterScanResultDto>> Handle(CenterScanCommand command, CancellationToken cancellationToken)
        {
            var data = await _attendanceService.ScanCenterCodeAsync(command.TenantId, command.Request.Token, cancellationToken);
            var message = data.AlreadyPresent ? "الطالب مسجّل حضوره بالفعل." : "تم تسجيل الحضور.";
            return Result<CenterScanResultDto>.Success(data, message);
        }
    }
}
