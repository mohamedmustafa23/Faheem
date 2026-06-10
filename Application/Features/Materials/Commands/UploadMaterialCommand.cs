using Application.Features.Materials.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Materials.Commands
{
    public class UploadMaterialCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public UploadMaterialRequest Request { get; set; } = null!;
    }

    public class UploadMaterialCommandHandler : IRequestHandler<UploadMaterialCommand, Result>
    {
        private readonly IMaterialService _materialService;
        public UploadMaterialCommandHandler(IMaterialService materialService) => _materialService = materialService;

        public async Task<Result> Handle(UploadMaterialCommand command, CancellationToken cancellationToken)
        {
            var result = await _materialService.UploadMaterialAsync(command.Request, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}