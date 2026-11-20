using ClinicBook.Application.Common.Interfaces;
using ClinicBook.Application.Features.Auth.DTOs;
using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Commands.Mfa;

public sealed class GetMfaSetupCommandHandler : IRequestHandler<GetMfaSetupCommand, Result<MfaSetupDto>>
{
    private readonly IAuthService _authService;
    public GetMfaSetupCommandHandler(IAuthService authService) => _authService = authService;
    public Task<Result<MfaSetupDto>> Handle(GetMfaSetupCommand request, CancellationToken cancellationToken)
        => _authService.GetMfaSetupAsync(request.UserId, cancellationToken);
}

public sealed class EnableMfaCommandHandler : IRequestHandler<EnableMfaCommand, Result<IReadOnlyCollection<string>>>
{
    private readonly IAuthService _authService;
    public EnableMfaCommandHandler(IAuthService authService) => _authService = authService;
    public Task<Result<IReadOnlyCollection<string>>> Handle(EnableMfaCommand request, CancellationToken cancellationToken)
        => _authService.EnableMfaAsync(request.UserId, request.Code, cancellationToken);
}

public sealed class DisableMfaCommandHandler : IRequestHandler<DisableMfaCommand, Result>
{
    private readonly IAuthService _authService;
    public DisableMfaCommandHandler(IAuthService authService) => _authService = authService;
    public Task<Result> Handle(DisableMfaCommand request, CancellationToken cancellationToken)
        => _authService.DisableMfaAsync(request.UserId, request.Code, cancellationToken);
}
