using Application.Features.Materials.Commands;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Application.Features.Materials.Validators
{
    public class UploadMaterialCommandValidator : AbstractValidator<UploadMaterialCommand>
    {
        public UploadMaterialCommandValidator()
        {
            RuleFor(x => x.Request.GroupIds).NotEmpty().WithMessage("You must select at least one group.");
            RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(150);

            RuleFor(x => x.Request.File)
                .NotNull().WithMessage("File is required.")
                .Must(BeWithinSizeLimit).WithMessage("File exceeds the maximum allowed size (20MB).");
        }

        private bool BeWithinSizeLimit(IFormFile file)
        {
            if (file == null) return false;
            const int maxFileSize = 20 * 1024 * 1024;
            return file.Length <= maxFileSize;
        }
    }
}