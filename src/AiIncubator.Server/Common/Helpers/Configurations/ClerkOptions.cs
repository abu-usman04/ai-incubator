namespace AiIncubator.Server.Common.Helpers.Configurations;

public class ClerkOptions
{
    public const string SectionName = "Clerk";

    /// <summary>OIDC authority (Clerk Frontend API URL). Falls back to <see cref="Issuer"/> when empty.</summary>
    public string Authority { get; init; } = string.Empty;

    /// <summary>Expected token issuer, e.g. https://your-app.clerk.accounts.dev</summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Optional expected audience. Audience validation is skipped when empty.</summary>
    public string Audience { get; init; } = string.Empty;
}
