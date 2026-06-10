using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Announcements.Commands
{
    public class DeleteAnnouncementCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public Guid AnnouncementId { get; set; }
    }

    public class DeleteAnnouncementCommandHandler : IRequestHandler<DeleteAnnouncementCommand, Result>
    {
        private readonly IAnnouncementService _announcementService;
        public DeleteAnnouncementCommandHandler(IAnnouncementService announcementService) => _announcementService = announcementService;

        public async Task<Result> Handle(DeleteAnnouncementCommand command, CancellationToken cancellationToken)
        {
            var result = await _announcementService.DeleteAnnouncementAsync(command.AnnouncementId, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}