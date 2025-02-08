using MediatR;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Validation.Abstractions;
using ValidationException = NArchitecture.Core.CrossCuttingConcerns.Exception.Types.ValidationException;

namespace NArchitecture.Core.Application.Pipelines.Validation;

/// <summary>
/// Pipeline behavior that performs validation on the incoming request using a registered validator.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IValidator<TRequest>? _validator;

    public ValidationBehavior(IValidator<TRequest>? validator = null)
    {
        _validator = validator;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (_validator is not null)
        {
            ValidationResult validationResult = _validator.Validate(request);

            if (!validationResult.IsValid && validationResult.Errors?.Any() == true)
                throw new ValidationException(validationResult.Errors);
        }

        TResponse response = await next();
        return response;
    }
}
