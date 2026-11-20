using FluentValidation;

namespace ClinicBook.Application.Features.Auth.Commands.Mfa;

public sealed class EnableMfaCommandValidator : AbstractValidator<EnableMfaCommand>
{
    public EnableMfaCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MinimumLength(6).MaximumLength(16);
    }
}

public sealed class DisableMfaCommandValidator : AbstractValidator<DisableMfaCommand>
{
    public DisableMfaCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Code).MaximumLength(32).When(x => !string.IsNullOrWhiteSpace(x.Code));
    }
}
