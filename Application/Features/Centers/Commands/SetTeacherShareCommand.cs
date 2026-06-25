using Application.Features.Centers.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Centers.Commands
{
    public class SetTeacherShareCommand : IRequest<Result<string>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public string OwnerUserId { get; set; } = string.Empty;
        [JsonIgnore] public string TeacherUserId { get; set; } = string.Empty;
        public SetTeacherShareRequest Request { get; set; } = new();
    }

    public class SetTeacherShareCommandHandler : IRequestHandler<SetTeacherShareCommand, Result<string>>
    {
        private readonly ICenterService _centerService;
        public SetTeacherShareCommandHandler(ICenterService centerService) => _centerService = centerService;

        public async Task<Result<string>> Handle(SetTeacherShareCommand command, CancellationToken cancellationToken)
        {
            var message = await _centerService.SetTeacherShareAsync(
                command.TenantId, command.OwnerUserId, command.TeacherUserId, command.Request.SharePercent, cancellationToken);
            return Result<string>.Success(message, message);
        }
    }
}
