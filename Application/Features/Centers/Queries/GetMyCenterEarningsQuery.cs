using Application.Features.Centers.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Centers.Queries
{
    // The current teacher's own earnings in the center they're operating in: the
    // share % the owner set, what was collected, and the teacher's net cut.
    public class GetMyCenterEarningsQuery : IRequest<Result<CenterTeacherDetailDto>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public string TeacherUserId { get; set; } = string.Empty;
    }

    public class GetMyCenterEarningsQueryHandler : IRequestHandler<GetMyCenterEarningsQuery, Result<CenterTeacherDetailDto>>
    {
        private readonly ICenterService _centerService;
        public GetMyCenterEarningsQueryHandler(ICenterService centerService) => _centerService = centerService;

        public async Task<Result<CenterTeacherDetailDto>> Handle(GetMyCenterEarningsQuery query, CancellationToken cancellationToken)
        {
            var data = await _centerService.GetMyCenterEarningsAsync(query.TenantId, query.TeacherUserId, cancellationToken);
            return Result<CenterTeacherDetailDto>.Success(data);
        }
    }
}
