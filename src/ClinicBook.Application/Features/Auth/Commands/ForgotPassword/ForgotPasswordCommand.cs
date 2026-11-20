using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;
