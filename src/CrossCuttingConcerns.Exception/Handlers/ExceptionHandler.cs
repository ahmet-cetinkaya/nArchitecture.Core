using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;

namespace NArchitecture.Core.CrossCuttingConcerns.Exception.Handlers;

/// <summary>
/// Provides a base class for handling different types of exceptions in the application.
/// </summary>
public abstract class ExceptionHandler
{
    public abstract Task HandleException(BusinessException businessException);
    public abstract Task HandleException(ValidationException validationException);
    public abstract Task HandleException(AuthorizationException authorizationException);
    public abstract Task HandleException(NotFoundException notFoundException);
    public abstract Task HandleException(System.Exception exception);
}
