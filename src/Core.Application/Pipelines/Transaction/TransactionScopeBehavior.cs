using System.Transactions;
using MediatR;

namespace NArchitecture.Core.Application.Pipelines.Transaction;

/// <summary>
/// Pipeline behavior that wraps the request handling in a transaction scope.
/// </summary>
/// <typeparam name="TRequest">The type of the request being handled</typeparam>
/// <typeparam name="TResponse">The type of the response from the handler</typeparam>
public class TransactionScopeBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ITransactionalRequest
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        // Create transaction scope with async flow enabled
        using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);
        try
        {
            TResponse response = await next();
            transactionScope.Complete();
            return response;
        }
        catch (Exception)
        {
            // Ensure transaction is disposed on exception
            transactionScope.Dispose();
            throw;
        }
    }
}
