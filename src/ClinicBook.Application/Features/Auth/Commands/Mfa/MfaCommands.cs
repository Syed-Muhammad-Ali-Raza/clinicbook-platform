using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Commands.Mfa;

public sealed record GetMfaSetupCommand(Guid UserId) : IRequest<Result<ClinicBook.Application.Features.Auth.DTOs.MfaSetupDto>>;

public sealed record EnableMfaCommand(Guid UserId, string Code) : IRequest<Result<IReadOnlyCollection<string>>>;

public sealed record DisableMfaCommand(Guid UserId, string? Code) : IRequest<Result>;
