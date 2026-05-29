using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using AiIncubator.Server.Common.Authentication;

namespace AiIncubator.Server.Common.Helpers.Configurations;

public static class AuthenticationRegistration
{
    public static IServiceCollection AddAiIncubatorAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ClerkOptions>(configuration.GetSection(ClerkOptions.SectionName));
        ClerkOptions clerk = configuration.GetSection(ClerkOptions.SectionName).Get<ClerkOptions>() ?? new ClerkOptions();

        bool clerkConfigured = !string.IsNullOrWhiteSpace(clerk.Issuer) || !string.IsNullOrWhiteSpace(clerk.Authority);

        if (clerkConfigured)
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = string.IsNullOrWhiteSpace(clerk.Authority) ? clerk.Issuer : clerk.Authority;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = !string.IsNullOrWhiteSpace(clerk.Issuer),
                        ValidIssuer = clerk.Issuer,
                        ValidateAudience = !string.IsNullOrWhiteSpace(clerk.Audience),
                        ValidAudience = clerk.Audience,
                        ValidateLifetime = true
                    };
                });
        }
        else
        {
            services
                .AddAuthentication(DevAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(
                    DevAuthenticationHandler.SchemeName,
                    _ => { });
        }

        services.AddAuthorization();
        return services;
    }
}
