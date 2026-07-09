using Application.Features.Tenancy.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Tenancy.Queries
{
    public class GetMySubscriptionQuery : IRequest<Result<SubscriptionStatusDto?>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
    }

    public class GetMySubscriptionQueryHandler : IRequestHandler<GetMySubscriptionQuery, Result<SubscriptionStatusDto?>>
    {
        private readonly ISubscriptionService _subscriptions;
        public GetMySubscriptionQueryHandler(ISubscriptionService subscriptions) => _subscriptions = subscriptions;

        public async Task<Result<SubscriptionStatusDto?>> Handle(GetMySubscriptionQuery request, CancellationToken ct)
            => Result<SubscriptionStatusDto?>.Success(await _subscriptions.GetStatusAsync(request.TenantId, ct));
    }
}
