using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Commands.Logout;

public sealed record LogoutCommand(string? RefreshToken, Guid? UserId, string? IpAddress) : IRequest<Result>;
