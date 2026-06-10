using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Materials.Commands
{
    public class DeleteMaterialCommand : IRequest<Result>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        public Guid MaterialId { get; set; }
    }

    public class DeleteMaterialCommandHandler : IRequestHandler<DeleteMaterialCommand, Result>
    {
        private readonly IMaterialService _materialService;
        public DeleteMaterialCommandHandler(IMaterialService materialService) => _materialService = materialService;

        public async Task<Result> Handle(DeleteMaterialCommand command, CancellationToken cancellationToken)
        {
            var result = await _materialService.DeleteMaterialAsync(command.MaterialId, command.TenantId, cancellationToken);
            return Result.Success(result);
        }
    }
}