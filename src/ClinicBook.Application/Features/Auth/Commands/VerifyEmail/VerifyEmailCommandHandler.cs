using ClinicBook.Application.Common.Interfaces;
using ClinicBook.Application.Features.Auth.DTOs;
using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result>
{
    private readonly IAuthService _authService;
    public VerifyEmailCommandHandler(IAuthService authService) => _authService = authService;

    public Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
        => _authService.VerifyEmailAsync(new VerifyEmailRequestDto(request.UserId, request.Token), cancellationToken);
}
