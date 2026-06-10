using Application.Features.Identity.Commands;
using FluentValidation;

namespace Application.Features.Identity.Validators
{
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.Request.Email)
                .NotEmpty().EmailAddress();

            RuleFor(x => x.Request.OtpCode)
                .NotEmpty().Length(6);

            RuleFor(x => x.Request.NewPassword)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches("[a-zA-Z]").WithMessage("Password must contain at least one letter"); 

            RuleFor(x => x.Request.ConfirmNewPassword)
                .Equal(x => x.Request.NewPassword)
                .WithMessage("Passwords do not match");
        }
    }
}