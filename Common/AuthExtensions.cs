using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Common;

public static class AuthExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication()
            .AddKeycloakJwtBearer(serviceName: "keycloack", realm: "joverflow", options =>
            {
                options.RequireHttpsMetadata = false;
                options.Audience = "joverflow";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuers =
                    [
                        "http://localhost:6001/realms/joverflow",
                        "http://keycloak/realms/joverflow",
                        "http://id.joverflow.local/realms/joverflow",
                    ]
                };
            });

        return services;
    }
}