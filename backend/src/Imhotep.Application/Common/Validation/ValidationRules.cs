using FluentValidation;

namespace Imhotep.Application.Common.Validation;

public static class ValidationRules
{
    /// <summary>Password policy shared by registration and invitation acceptance.</summary>
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty()
        .MinimumLength(12).WithMessage("Password must be at least 12 characters.")
        .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
        .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
        .Matches("[0-9]").WithMessage("Password must contain a digit.")
        .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a symbol.");
}
