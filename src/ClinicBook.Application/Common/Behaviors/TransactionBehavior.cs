#pragma warning disable CA1848
using ClinicBook.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClinicBook.Application.Common.Behaviors;

/// <summary>
/// Wraps transactional commands in a persistence transaction.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    /// <summary>Initializes the behavior.</summary>
    public TransactionBehavior(IUnitOfWork unitOfWork, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ITransactionalCommand)
        {
            return await next().ConfigureAwait(false);
        }

        var requestName = typeof(TRequest).Name;
        _logger.LogDebug("Beginning transaction for {RequestName}", requestName);

        // In Phase 1, SaveChanges is the commit boundary. Explicit DB transactions are added with EF Core in later phases.
        var response = await next().ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Committed transaction for {RequestName}", requestName);
        return response;
    }
}
#pragma warning restore CA1848
