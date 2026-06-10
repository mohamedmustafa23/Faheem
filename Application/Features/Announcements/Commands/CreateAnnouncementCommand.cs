using Application.Features.Announcements.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Announcements.Commands
{
    public class CreateAnnouncementCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public CreateAnnouncementRequest Request { get; set; } = new();
    }

    public class CreateAnnouncementCommandHandler : IRequestHandler<CreateAnnouncementCommand, Result>
    {
        private readonly IAnnouncementService _announcementService;
        public CreateAnnouncementCommandHandler(IAnnouncementService announcementService) => _announcementService = announcementService;

        public async Task<Result> Handle(CreateAnnouncementCommand command, CancellationToken cancellationToken)
        {
            var result = await _announcementService.CreateAnnouncementAsync(command.Request, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}