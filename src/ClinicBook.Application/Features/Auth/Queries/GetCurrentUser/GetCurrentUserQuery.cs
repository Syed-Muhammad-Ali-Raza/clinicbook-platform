using ClinicBook.Application.Features.Auth.DTOs;
using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<Result<CurrentUserDto>>;
