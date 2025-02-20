using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Security.Abstractions.Authentication;
using NArchitecture.Core.Security.Abstractions.Authentication.Entities;
using NArchitecture.Core.Security.Abstractions.Authentication.Models;
using NArchitecture.Core.Security.Abstractions.Authorization;
using NArchitecture.Core.Security.Abstractions.Authorization.Entities;
using NArchitecture.Core.Security.Authorization.Extensions;

namespace NArchitecture.Core.Security.Authentication;

public class JwtAuthenticationService<TUserId, TOperationClaimId, TRefreshTokenId, TUserAuthenticatorId>(
    IRefreshTokenRepository<TRefreshTokenId, TUserId, TUserAuthenticatorId> refreshTokenRepository,
    IUserRepository<TUserId, TUserAuthenticatorId, TOperationClaimId> userRepository,
    IAuthorizationService<TUserId, TOperationClaimId> authorizationService,
    IJwtAuthenticationConfiguration configuration
) : IAuthenticationService<TUserId, TUserAuthenticatorId>
{
    protected readonly IRefreshTokenRepository<TRefreshTokenId, TUserId, TUserAuthenticatorId> RefreshTokenRepository =
        refreshTokenRepository;
    protected readonly IUserRepository<TUserId, TUserAuthenticatorId, TOperationClaimId> UserRepository = userRepository;
    protected readonly IAuthorizationService<TUserId, TOperationClaimId> AuthorizationService = authorizationService;
    protected readonly IJwtAuthenticationConfiguration Configuration = configuration;

    protected virtual Token CreateAccessToken(
        User<TUserId, TUserAuthenticatorId> user,
        ICollection<OperationClaim<TOperationClaimId>> operationClaims
    )
    {
        DateTime issuedAt = DateTime.UtcNow;
        DateTime expiresAt = issuedAt.Add(Configuration.AccessTokenExpiration);
        SecurityKey securityKey = CreateSecurityKey(Configuration.SecurityKey);
        SigningCredentials signingCredentials = CreateSigningCredentials(securityKey);

        JwtSecurityToken jwt = new(
            issuer: Configuration.Issuer,
            audience: Configuration.Audience,
            claims: GetClaims(user, operationClaims),
            notBefore: issuedAt,
            expires: expiresAt,
            signingCredentials: signingCredentials
        );

        string token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return new(token, expiresAt);
    }

    protected virtual SecurityKey CreateSecurityKey(string securityKey)
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));
    }

    protected virtual SigningCredentials CreateSigningCredentials(SecurityKey securityKey)
    {
        return new(securityKey, SecurityAlgorithms.HmacSha512Signature);
    }

    protected virtual RefreshToken<TRefreshTokenId, TUserId, TUserAuthenticatorId> CreateRefreshToken(
        User<TUserId, TUserAuthenticatorId> user,
        string ipAddress
    )
    {
        byte[] numberByte = new byte[32];
        using var random = RandomNumberGenerator.Create();
        random.GetBytes(numberByte);
        string refreshToken = Convert.ToBase64String(numberByte);

        return new(user.Id!, refreshToken, DateTime.UtcNow.Add(Configuration.RefreshTokenTTL), ipAddress);
    }

    protected virtual IEnumerable<Claim> GetClaims(
        User<TUserId, TUserAuthenticatorId> user,
        ICollection<OperationClaim<TOperationClaimId>> operationClaims
    )
    {
        List<Claim> claims = [];
        claims.AddUserId(user.Id);
        claims.AddOperationClaim(operationClaims.Select(claim => claim.Name).ToArray());
        return claims;
    }

    protected virtual bool CheckPassword(User<TUserId, TUserAuthenticatorId> user, string password)
    {
        byte[] passwordHash = user.PasswordHash ?? [];
        byte[] passwordSalt = user.PasswordSalt ?? [];

        using HMACSHA512 hmac = new(passwordSalt);
        byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return computedHash.SequenceEqual(passwordHash);
    }

    public virtual async Task<AuthenticationResponse> LoginAsync(
        LoginRequest<TUserId, TUserAuthenticatorId> request,
        CancellationToken cancellationToken = default
    )
    {
        if (!CheckPassword(request.User, request.Password))
            throw new BusinessException(await Configuration.GetInvalidPasswordMessageAsync(cancellationToken));

        ICollection<OperationClaim<TOperationClaimId>> operationClaims = await AuthorizationService.GetUserOperationClaimsAsync(
            request.User.Id!,
            cancellationToken
        );
        Token accessToken = CreateAccessToken(request.User, operationClaims);
        RefreshToken<TRefreshTokenId, TUserId, TUserAuthenticatorId> refreshTokenEntity = CreateRefreshToken(
            request.User,
            request.IpAddress
        );

        _ = await RefreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        Token refreshToken = new(refreshTokenEntity.Token, refreshTokenEntity.ExpiresAt);
        return new(accessToken, refreshToken);
    }

    public virtual async Task<RefreshTokenResponse> RefreshTokenAsync(
        string refreshToken,
        string ipAddress,
        CancellationToken cancellationToken = default
    )
    {
        RefreshToken<TRefreshTokenId, TUserId, TUserAuthenticatorId> token =
            await RefreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken)
            ?? throw new BusinessException(await Configuration.GetInvalidRefreshTokenMessageAsync(cancellationToken));

        if (token.RevokedAt.HasValue)
            throw new BusinessException(await Configuration.GetTokenRevokedMessageAsync(cancellationToken));
        if (token.ExpiresAt <= DateTime.UtcNow)
            throw new BusinessException(await Configuration.GetTokenExpiredMessageAsync(cancellationToken));

        User<TUserId, TUserAuthenticatorId> user =
            await UserRepository.GetByIdAsync(token.UserId, cancellationToken: cancellationToken)
            ?? throw new BusinessException(await Configuration.GetUserNotFoundMessageAsync(cancellationToken));

        ICollection<OperationClaim<TOperationClaimId>> operationClaims = await AuthorizationService.GetUserOperationClaimsAsync(
            user.Id!,
            cancellationToken
        );
        Token newAccessToken = CreateAccessToken(user, operationClaims);
        RefreshToken<TRefreshTokenId, TUserId, TUserAuthenticatorId> newRefreshTokenEntity = CreateRefreshToken(user, ipAddress);

        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReplacedByToken = newRefreshTokenEntity.Token;
        _ = await RefreshTokenRepository.UpdateAsync(token, cancellationToken);

        _ = await RefreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);

        Token newRefreshToken = new(newRefreshTokenEntity.Token, newRefreshTokenEntity.ExpiresAt);
        return new(newAccessToken, newRefreshToken);
    }

    public virtual async Task RevokeRefreshTokenAsync(
        string refreshToken,
        string ipAddress,
        string? reason = null,
        CancellationToken cancellationToken = default
    )
    {
        RefreshToken<TRefreshTokenId, TUserId, TUserAuthenticatorId> token =
            await RefreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken)
            ?? throw new BusinessException(await Configuration.GetInvalidRefreshTokenMessageAsync(cancellationToken));

        if (token.RevokedAt.HasValue)
            throw new BusinessException(await Configuration.GetTokenAlreadyRevokedMessageAsync(cancellationToken));

        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReasonRevoked = reason;

        _ = await RefreshTokenRepository.UpdateAsync(token, cancellationToken);
    }

    public virtual async Task RevokeAllRefreshTokensAsync(
        TUserId userId,
        string? reason = null,
        CancellationToken cancellationToken = default
    )
    {
        ICollection<RefreshToken<TRefreshTokenId, TUserId, TUserAuthenticatorId>> activeTokens =
            await RefreshTokenRepository.GetAllActiveByUserIdAsync(userId, cancellationToken);

        foreach (RefreshToken<TRefreshTokenId, TUserId, TUserAuthenticatorId> token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.ReasonRevoked = reason;
            _ = await RefreshTokenRepository.UpdateAsync(token, cancellationToken);
        }
    }
}
