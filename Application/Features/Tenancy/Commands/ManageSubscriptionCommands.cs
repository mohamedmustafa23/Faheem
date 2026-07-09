using Application.Features.Tenancy.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Tenancy.Commands
{
    public class ExtendSubscriptionCommand : IRequest<Result<AdminSubscriberDto>>
    {
        [JsonIgnore] public string Id { get; set; } = string.Empty;
        public int Months { get; set; }
        public int? MaxTeachers { get; set; }
    }

    public class ExtendSubscriptionCommandHandler : IRequestHandler<ExtendSubscriptionCommand, Result<AdminSubscriberDto>>
    {
        private readonly IAdminService _admin;
        public ExtendSubscriptionCommandHandler(IAdminService admin) => _admin = admin;

        public async Task<Result<AdminSubscriberDto>> Handle(ExtendSubscriptionCommand request, CancellationToken ct)
            => Result<AdminSubscriberDto>.Success(await _admin.ExtendSubscriptionAsync(request.Id, request.Months, request.MaxTeachers, ct));
    }

    public class SetSubscriberActiveCommand : IRequest<Result<AdminSubscriberDto>>
    {
        [JsonIgnore] public string Id { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class SetSubscriberActiveCommandHandler : IRequestHandler<SetSubscriberActiveCommand, Result<AdminSubscriberDto>>
    {
        private readonly IAdminService _admin;
        public SetSubscriberActiveCommandHandler(IAdminService admin) => _admin = admin;

        public async Task<Result<AdminSubscriberDto>> Handle(SetSubscriberActiveCommand request, CancellationToken ct)
            => Result<AdminSubscriberDto>.Success(await _admin.SetActiveAsync(request.Id, request.IsActive, ct));
    }

    public class SetCenterSeatsCommand : IRequest<Result<AdminSubscriberDto>>
    {
        [JsonIgnore] public string Id { get; set; } = string.Empty;
        public int? MaxTeachers { get; set; }
    }

    public class SetCenterSeatsCommandHandler : IRequestHandler<SetCenterSeatsCommand, Result<AdminSubscriberDto>>
    {
        private readonly IAdminService _admin;
        public SetCenterSeatsCommandHandler(IAdminService admin) => _admin = admin;

        public async Task<Result<AdminSubscriberDto>> Handle(SetCenterSeatsCommand request, CancellationToken ct)
            => Result<AdminSubscriberDto>.Success(await _admin.SetCenterSeatsAsync(request.Id, request.MaxTeachers, ct));
    }

    public class DeleteSubscriberCommand : IRequest<Result>
    {
        [JsonIgnore] public string Id { get; set; } = string.Empty;
    }

    public class DeleteSubscriberCommandHandler : IRequestHandler<DeleteSubscriberCommand, Result>
    {
        private readonly IAdminService _admin;
        public DeleteSubscriberCommandHandler(IAdminService admin) => _admin = admin;

        public async Task<Result> Handle(DeleteSubscriberCommand request, CancellationToken ct)
        {
            await _admin.DeleteSubscriberAsync(request.Id, ct);
            return Result.Success("تم حذف المشترك.");
        }
    }
}
