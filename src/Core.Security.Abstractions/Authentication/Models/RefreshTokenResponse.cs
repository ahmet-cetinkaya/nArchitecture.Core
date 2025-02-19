namespace NArchitecture.Core.Security.Abstractions.Authentication.Models;

public readonly record struct RefreshTokenResponse(Token AccessToken, Token RefreshToken);
