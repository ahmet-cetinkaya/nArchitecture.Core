namespace NArchitecture.Core.Security.Abstractions.Authentication.Models;

public readonly record struct AuthenticationResponse(Token AccessToken, Token RefreshToken);
