using System.Transactions;
using NArchitecture.Core.Application.Pipelines.Transaction;
using NArchitecture.Core.Mediator.Abstractions;
using Shouldly;

namespace NArchitecture.Core.Application.Tests.Pipelines.Transaction;

[Trait("Category", "Transaction")]
public class TransactionScopeBehaviorTests
{
    private readonly TransactionScopeBehavior<TestRequest, TestResponse> _behavior;

    public TransactionScopeBehaviorTests()
    {
        _behavior = new();
    }

    [Fact(DisplayName = "Handle should complete transaction when operation succeeds")]
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

    [Fact(DisplayName = "Handle should rollback transaction when operation throws exception")]
    public async Task Handle_WhenExceptionOccurs_ShouldRollbackTransaction()
    {
        // Arrange
        var request = new TestRequest();
        var expectedException = new InvalidOperationException("Test exception");
        Task<TestResponse> next() => throw expectedException;

        // Act & Assert
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _behavior.Handle(request, next, CancellationToken.None)
        );

        exception.ShouldBe(expectedException);
    }

    [Fact(DisplayName = "Handle should handle nested transactions correctly")]
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

    [Fact(DisplayName = "Handle should maintain transaction during long running operations")]
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

    private class TestRequest : IRequest<TestResponse>, ITransactionalRequest { }

    private class TestResponse { }
}
