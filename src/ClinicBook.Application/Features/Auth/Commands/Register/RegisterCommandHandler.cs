using ClinicBook.Application.Common.Interfaces;
using ClinicBook.Application.Features.Auth.DTOs;
using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResultDto>>
{
    private readonly IAuthService _authService;

    public RegisterCommandHandler(IAuthService authService) => _authService = authService;

    public Task<Result<AuthResultDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var dto = new RegisterRequestDto(request.Email, request.Password, request.FirstName, request.LastName, request.PhoneNumber);
        return _authService.RegisterAsync(dto, request.IpAddress, request.DeviceInfo, cancellationToken);
    }
}
