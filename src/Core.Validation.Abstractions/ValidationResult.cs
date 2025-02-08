namespace NArchitecture.Core.Validation.Abstractions;

public struct ValidationResult
{
    public bool IsValid { get; set; }
    public IEnumerable<ValidationError>? Errors { get; set; }
}
