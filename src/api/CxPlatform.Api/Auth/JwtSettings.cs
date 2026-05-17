namespace CxPlatform.Api.Auth;

public class JwtSettings
{
    public string Issuer { get; set; } = "cx-platform";
    public string Audience { get; set; } = "cx-platform";
    // HS256 dev secret — replace with RS256 keypair in production via secret store.
    public string Secret { get; set; } = "dev-only-do-not-use-in-production-change-me-please-32+chars";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 14;
}
