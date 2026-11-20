using ClinicBook.Application.Features.Auth.DTOs;
using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Commands.Refresh;

public sealed record RefreshTokenCommand(string? RefreshToken, string? IpAddress, string? DeviceInfo) : IRequest<Result<AuthResultDto>>;
