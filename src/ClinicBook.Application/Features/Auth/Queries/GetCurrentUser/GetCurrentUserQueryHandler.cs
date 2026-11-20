using ClinicBook.Application.Common.Interfaces;
using ClinicBook.Application.Features.Auth.DTOs;
using ClinicBook.Domain.Common;
using MediatR;

namespace ClinicBook.Application.Features.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserDto>>
{
    private readonly IAuthService _authService;

    public GetCurrentUserQueryHandler(IAuthService authService) => _authService = authService;

    public Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        => _authService.GetCurrentUserAsync(request.UserId, cancellationToken);
}
