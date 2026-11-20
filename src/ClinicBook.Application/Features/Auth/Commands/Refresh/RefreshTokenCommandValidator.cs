using FluentValidation;

namespace ClinicBook.Application.Features.Auth.Commands.Refresh;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        // Refresh token is read from HttpOnly cookie by the controller; command value may be empty here.
        // Keep validator permissive and let service enforce presence/validity.
        RuleFor(x => x.RefreshToken).MaximumLength(4096).When(x => !string.IsNullOrWhiteSpace(x.RefreshToken));
    }
}
