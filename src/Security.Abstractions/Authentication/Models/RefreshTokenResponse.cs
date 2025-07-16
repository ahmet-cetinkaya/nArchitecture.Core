namespace NArchitecture.Core.Security.Abstractions.Authentication.Models;

public record RefreshTokenResponse(Token AccessToken, Token RefreshToken);
