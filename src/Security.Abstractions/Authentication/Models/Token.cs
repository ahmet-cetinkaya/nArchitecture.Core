namespace NArchitecture.Core.Security.Abstractions.Authentication.Models;

public record struct Token(string Content, DateTime ExpiresAt);
