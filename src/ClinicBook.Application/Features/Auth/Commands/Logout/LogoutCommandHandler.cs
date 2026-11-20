using ClinicBook.Application.Common.Interfaces;
using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IAuthService _authService;
    public LogoutCommandHandler(IAuthService authService) => _authService = authService;

    public Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
        => _authService.LogoutAsync(request.RefreshToken, request.UserId, request.IpAddress, cancellationToken);
}
