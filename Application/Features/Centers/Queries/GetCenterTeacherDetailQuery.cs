using Application.Features.Centers.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Centers.Queries
{
    public class GetCenterTeacherDetailQuery : IRequest<Result<CenterTeacherDetailDto>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public string OwnerUserId { get; set; } = string.Empty;
        [JsonIgnore] public string TeacherUserId { get; set; } = string.Empty;
    }

    public class GetCenterTeacherDetailQueryHandler : IRequestHandler<GetCenterTeacherDetailQuery, Result<CenterTeacherDetailDto>>
    {
        private readonly ICenterService _centerService;
        public GetCenterTeacherDetailQueryHandler(ICenterService centerService) => _centerService = centerService;

        public async Task<Result<CenterTeacherDetailDto>> Handle(GetCenterTeacherDetailQuery query, CancellationToken cancellationToken)
        {
            var data = await _centerService.GetCenterTeacherDetailAsync(query.TenantId, query.OwnerUserId, query.TeacherUserId, cancellationToken);
            return Result<CenterTeacherDetailDto>.Success(data);
        }
    }
}
