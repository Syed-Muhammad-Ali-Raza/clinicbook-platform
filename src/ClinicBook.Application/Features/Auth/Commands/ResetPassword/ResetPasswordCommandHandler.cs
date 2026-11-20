using ClinicBook.Application.Common.Interfaces;
using ClinicBook.Application.Features.Auth.DTOs;
using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IAuthService _authService;
    public ResetPasswordCommandHandler(IAuthService authService) => _authService = authService;

    public Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        => _authService.ResetPasswordAsync(new ResetPasswordRequestDto(request.UserId, request.Token, request.NewPassword), cancellationToken);
}
