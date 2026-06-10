using Application.Features.Identity.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Identity.Queries
{
    public class GetTeacherAssistantsQuery : IRequest<Result<List<AssistantDto>>>
    {
        [JsonIgnore] public string TeacherTenantId { get; set; } = string.Empty;
    }

    public class GetTeacherAssistantsQueryHandler : IRequestHandler<GetTeacherAssistantsQuery, Result<List<AssistantDto>>>
    {
        private readonly IAuthService _authService;
        public GetTeacherAssistantsQueryHandler(IAuthService authService) => _authService = authService;

        public async Task<Result<List<AssistantDto>>> Handle(GetTeacherAssistantsQuery query, CancellationToken cancellationToken)
        {
            var data = await _authService.GetTeacherAssistantsAsync(query.TeacherTenantId, cancellationToken);
            return Result<List<AssistantDto>>.Success(data);
        }
    }
}
