using Application.Exceptions;
using Application.Features.Tenancy.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Tenancy.Queries
{
    public class GetSubscribersQuery : IRequest<Result<List<AdminSubscriberDto>>> { }

    public class GetSubscribersQueryHandler : IRequestHandler<GetSubscribersQuery, Result<List<AdminSubscriberDto>>>
    {
        private readonly IAdminService _admin;
        public GetSubscribersQueryHandler(IAdminService admin) => _admin = admin;

        public async Task<Result<List<AdminSubscriberDto>>> Handle(GetSubscribersQuery request, CancellationToken ct)
            => Result<List<AdminSubscriberDto>>.Success(await _admin.GetSubscribersAsync(ct));
    }

    public class GetSubscriberByIdQuery : IRequest<Result<AdminSubscriberDto>>
    {
        public string Id { get; set; } = string.Empty;
    }

    public class GetSubscriberByIdQueryHandler : IRequestHandler<GetSubscriberByIdQuery, Result<AdminSubscriberDto>>
    {
        private readonly IAdminService _admin;
        public GetSubscriberByIdQueryHandler(IAdminService admin) => _admin = admin;

        public async Task<Result<AdminSubscriberDto>> Handle(GetSubscriberByIdQuery request, CancellationToken ct)
        {
            var dto = await _admin.GetSubscriberByIdAsync(request.Id, ct)
                ?? throw new NotFoundException(["المشترك مش موجود."]);
            return Result<AdminSubscriberDto>.Success(dto);
        }
    }
}
