using ClinicBook.Application.Common.Interfaces;
using ClinicBook.Application.Features.Auth.DTOs;
using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResultDto>>
{
    private readonly IAuthService _authService;
    public LoginCommandHandler(IAuthService authService) => _authService = authService;

    public Task<Result<AuthResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var dto = new LoginRequestDto(request.Email, request.Password, request.TwoFactorCode, request.RecoveryCode);
        return _authService.LoginAsync(dto, request.IpAddress, request.DeviceInfo, cancellationToken);
    }
}
