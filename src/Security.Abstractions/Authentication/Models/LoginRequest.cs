using NArchitecture.Core.Security.Abstractions.Authentication.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authentication.Models;

public readonly record struct LoginRequest<TUserId, TUserAuthenticatorId>(
    User<TUserId, TUserAuthenticatorId> User,
    string Password,
    string IpAddress
);
