namespace NArchitecture.Core.Security.Abstractions.Authentication.Models;

public record AuthenticationResponse(Token AccessToken, Token RefreshToken);
