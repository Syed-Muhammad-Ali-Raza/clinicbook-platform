using ClinicBook.Application.Common.Interfaces;
using ClinicBook.Application.Features.Auth.DTOs;
using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IAuthService _authService;
    public ForgotPasswordCommandHandler(IAuthService authService) => _authService = authService;

    public Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        => _authService.ForgotPasswordAsync(new ForgotPasswordRequestDto(request.Email), cancellationToken);
}
