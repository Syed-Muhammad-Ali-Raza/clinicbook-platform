using ClinicBook.Application.Common.Interfaces;
using ClinicBook.Application.Features.Auth.DTOs;
using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Commands.Refresh;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResultDto>>
{
    private readonly IAuthService _authService;
    public RefreshTokenCommandHandler(IAuthService authService) => _authService = authService;

    public Task<Result<AuthResultDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        => _authService.RefreshAsync(request.RefreshToken ?? string.Empty, request.IpAddress, request.DeviceInfo, cancellationToken);
}
