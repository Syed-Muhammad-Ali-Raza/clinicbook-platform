using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Commands.ResetPassword;

public sealed record ResetPasswordCommand(string UserId, string Token, string NewPassword) : IRequest<Result>;
