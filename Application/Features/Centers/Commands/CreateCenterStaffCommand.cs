using Application.Features.Centers.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Centers.Commands
{
    public class CreateCenterStaffCommand : IRequest<Result<string>>
    {
        [JsonIgnore] public string TenantId { get; set; } = string.Empty;
        [JsonIgnore] public string OwnerUserId { get; set; } = string.Empty;
        public RegisterCenterStaffRequest Request { get; set; } = new();
    }

    public class CreateCenterStaffCommandHandler : IRequestHandler<CreateCenterStaffCommand, Result<string>>
    {
        private readonly ICenterService _centerService;
        public CreateCenterStaffCommandHandler(ICenterService centerService) => _centerService = centerService;

        public async Task<Result<string>> Handle(CreateCenterStaffCommand command, CancellationToken cancellationToken)
        {
            var id = await _centerService.CreateStaffAsync(command.TenantId, command.OwnerUserId, command.Request, cancellationToken);
            return Result<string>.Success(id, "تم إنشاء حساب الموظف.");
        }
    }
}
