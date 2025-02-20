using System.Transactions;
using MediatR;
using NArchitecture.Core.Application.Pipelines.Transaction;
using Shouldly;

namespace NArchitecture.Core.Application.Tests.Pipelines.Transaction;

public class TransactionScopeBehaviorTests
{
    private readonly TransactionScopeBehavior<TestRequest, TestResponse> _behavior;

    public TransactionScopeBehaviorTests()
    {
        _behavior = new();
    }

    /// <summary>
    /// Should complete transaction when operation succeeds.
    /// </summary>
    [Fact]
    public async Task Handle_WhenSuccessful_ShouldCompleteTransaction()
    {
        // Arrange
        var request = new TestRequest();
        var expectedResponse = new TestResponse();
        Task<TestResponse> next() => Task.FromResult(expectedResponse);

        // Act
        TestResponse result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        _ = result.ShouldNotBeNull();
        result.ShouldBe(expectedResponse);
    }

    /// <summary>
    /// Should rollback transaction when operation throws exception.
    /// </summary>
    [Fact]
    public async Task Handle_WhenExceptionOccurs_ShouldRollbackTransaction()
    {
        // Arrange
        var request = new TestRequest();
        var expectedException = new InvalidOperationException("Test exception");
        Task<TestResponse> next() => throw expectedException;

        // Act & Assert
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _behavior.Handle(request, next, CancellationToken.None)
        );

        exception.ShouldBe(expectedException);
    }

    /// <summary>
    /// Should handle nested transaction scopes correctly.
    /// </summary>
    [Fact]
    public async Task Handle_WithNestedTransactions_ShouldHandleCorrectly()
    {
        // Arrange
        var request = new TestRequest();
        var expectedResponse = new TestResponse();
        async Task<TestResponse> next()
        {
            using var nestedScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            await Task.Delay(100); // Simulate some work
            nestedScope.Complete();
            return expectedResponse;
        }

        // Act
        TestResponse result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.ShouldBe(expectedResponse);
    }

    /// <summary>
    /// Should maintain transaction throughout long-running operations.
    /// </summary>
    [Fact]
    public async Task Handle_WithLongRunningOperation_ShouldMaintainTransaction()
    {
        // Arrange
        var request = new TestRequest();
        var expectedResponse = new TestResponse();
        bool transactionExists = false;

        async Task<TestResponse> next()
        {
            await Task.Delay(1000); // Simulate long-running operation
            transactionExists = System.Transactions.Transaction.Current != null;
            return expectedResponse;
        }

        // Act
        TestResponse result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.ShouldBe(expectedResponse);
        transactionExists.ShouldBeTrue("Transaction should have been active during operation");
    }

    // Test request classes
    private class TestRequest : IRequest<TestResponse>, ITransactionalRequest { }

    private class TestResponse { }
}
