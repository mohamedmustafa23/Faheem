using Application.Features.Notifications.Commands;
using FluentValidation;

namespace Application.Features.Notifications.Validators
{
    public class SaveDeviceTokenCommandValidator : AbstractValidator<SaveDeviceTokenCommand>
    {
        public SaveDeviceTokenCommandValidator()
        {
            RuleFor(x => x.Request.FcmToken).NotEmpty().WithMessage("FCM Token is required.");
        }
    }
}