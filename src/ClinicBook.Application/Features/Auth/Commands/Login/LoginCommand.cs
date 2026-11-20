using ClinicBook.Application.Features.Auth.DTOs;
using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password, string? TwoFactorCode, string? RecoveryCode, string? IpAddress, string? DeviceInfo)
    : IRequest<Result<AuthResultDto>>;
