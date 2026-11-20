using ClinicBook.Application.Features.Auth.DTOs;
using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(string Email, string Password, string FirstName, string LastName, string? PhoneNumber, string? IpAddress, string? DeviceInfo)
    : IRequest<Result<AuthResultDto>>;
