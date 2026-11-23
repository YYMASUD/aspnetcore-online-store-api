using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StoreApi.Application.Common.Models;
using StoreApi.Domain.Constants;

namespace StoreApi.Api.Extensions;

/// <summary>JWT bearer, CORS and rate-limiting registration for the API.</summary>
public static class SecurityExtensions
{
    /// <summary>Configures HS256 bearer authentication with role claim mapping.</summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwt.Key))
        {
            throw new InvalidOperationException(
                "Jwt:Key is not configured. Set it via user secrets or the JWT__KEY environment variable.");
        }

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(UserRoles.Admin));
            options.AddPolicy("CustomerOnly", policy => policy.RequireRole(UserRoles.Customer));
        });

        return services;
    }

    /// <summary>Adds the permissive CORS policy used by local frontend apps.</summary>
    public static IServiceCollection AddFrontendCors(this IServiceCollection services) =>
        services.AddCors(options =>
            options.AddPolicy("Frontend", policy =>
                policy.WithOrigins("http://localhost:3000", "http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()));

    /// <summary>Applies a 10-requests-per-minute fixed window to auth endpoints.</summary>
    public static IServiceCollection AddAuthRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // The policy must always be registered because AuthController declares it by name.
            // In the in-process integration test host a single fixture issues far more than
            // ten auth requests, so the limit is raised to effectively unlimited there.
            var isTesting = string.Equals(
                configuration["ASPNETCORE_ENVIRONMENT"], "Testing", StringComparison.OrdinalIgnoreCase);

            var permitLimit = isTesting
                ? int.MaxValue
                : configuration.GetValue("RateLimiting:Auth:PermitLimit", 10);

            options.AddFixedWindowLimiter("auth", limiter =>
            {
                limiter.PermitLimit = permitLimit;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                limiter.QueueLimit = 0;
            });
        });
}
