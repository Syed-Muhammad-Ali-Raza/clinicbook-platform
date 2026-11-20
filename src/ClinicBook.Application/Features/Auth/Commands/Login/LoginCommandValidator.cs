using FluentValidation;

namespace ClinicBook.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
        RuleFor(x => x.TwoFactorCode).MaximumLength(32).When(x => !string.IsNullOrWhiteSpace(x.TwoFactorCode));
        RuleFor(x => x.RecoveryCode).MaximumLength(64).When(x => !string.IsNullOrWhiteSpace(x.RecoveryCode));
    }
}
