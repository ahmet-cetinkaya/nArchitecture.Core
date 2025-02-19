namespace NArchitecture.Core.Test.Application.Constants;

/// <summary>
/// Contains constant validation error codes used in testing.
/// </summary>
public static class ValidationErrorCodes
{
    /// <summary>
    /// Error code for empty value validation failures.
    /// </summary>
    public static string NotEmptyValidator => "NotEmptyValidator";

    /// <summary>
    /// Error code for minimum length validation failures.
    /// </summary>
    public static string MinimumLengthValidator => "MinimumLengthValidator";

    /// <summary>
    /// Error code for email format validation failures.
    /// </summary>
    public static string EmailValidator => "EmailValidator";
}
