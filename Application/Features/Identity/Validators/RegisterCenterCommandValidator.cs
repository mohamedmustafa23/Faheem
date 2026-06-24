using Application.Features.Identity.Commands;
using FluentValidation;

namespace Application.Features.Identity.Validators
{
    public class RegisterCenterCommandValidator : AbstractValidator<RegisterCenterCommand>
    {
        public RegisterCenterCommandValidator()
        {
            RuleFor(x => x.Request.CenterName)
                .NotEmpty().WithMessage("Center name is required")
                .MaximumLength(100);

            RuleFor(x => x.Request.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(60);

            RuleFor(x => x.Request.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(60);

            RuleFor(x => x.Request.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Request.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required")
                .Matches(@"^[0-9]{11}$")
                .WithMessage("Phone number must be 11 digits");

            RuleFor(x => x.Request.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches("[a-zA-Z]").WithMessage("Password must contain at least one letter");

            RuleFor(x => x.Request.ConfirmPassword)
                .Equal(x => x.Request.Password)
                .WithMessage("Passwords do not match");
        }
    }
}
