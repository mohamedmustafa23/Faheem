using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Announcements.Commands
{
    public class TogglePinCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public Guid AnnouncementId { get; set; }
    }

    public class TogglePinCommandHandler : IRequestHandler<TogglePinCommand, Result>
    {
        private readonly IAnnouncementService _announcementService;
        public TogglePinCommandHandler(IAnnouncementService announcementService) => _announcementService = announcementService;

        public async Task<Result> Handle(TogglePinCommand command, CancellationToken cancellationToken)
        {
            var result = await _announcementService.TogglePinAsync(command.AnnouncementId, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}