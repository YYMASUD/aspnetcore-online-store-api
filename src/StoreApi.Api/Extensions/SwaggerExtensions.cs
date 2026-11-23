using System.Reflection;
using Microsoft.OpenApi.Models;

namespace StoreApi.Api.Extensions;

/// <summary>Swagger/OpenAPI registration with a JWT bearer "Authorize" button and XML comments.</summary>
public static class SwaggerExtensions
{
    /// <summary>Adds Swagger generation including the bearer security scheme.</summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services) =>
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Online Store API",
                Version = "v1",
                Description =
                    "A production-ready Online Store REST API with JWT authentication, " +
                    "role-based access control, cart and order management.",
                Contact = new OpenApiContact { Name = "StoreApi" },
                License = new OpenApiLicense { Name = "MIT" }
            });

            var scheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Paste the raw JWT here (without the 'Bearer ' prefix).",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            };

            options.AddSecurityDefinition("Bearer", scheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [scheme] = Array.Empty<string>()
            });

            var xmlFiles = new[]
            {
                Assembly.GetExecutingAssembly(),
                typeof(StoreApi.Application.DependencyInjection).Assembly,
                typeof(StoreApi.Domain.Entities.Product).Assembly
            };

            foreach (var assembly in xmlFiles)
            {
                var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                }
            }
        });
}
