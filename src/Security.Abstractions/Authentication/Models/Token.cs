namespace NArchitecture.Core.Security.Abstractions.Authentication.Models;

public readonly record struct Token(string Content, DateTime ExpiresAt);
