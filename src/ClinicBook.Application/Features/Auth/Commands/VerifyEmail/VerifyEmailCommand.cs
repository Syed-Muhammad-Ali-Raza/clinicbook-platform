using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Commands.VerifyEmail;

public sealed record VerifyEmailCommand(string UserId, string Token) : IRequest<Result>;
