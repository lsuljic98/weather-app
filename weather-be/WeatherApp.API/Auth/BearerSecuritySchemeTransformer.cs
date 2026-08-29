using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WeatherApp.API.Auth;

/// <summary>
/// Adds a JWT Bearer security scheme to the OpenAPI document so Swagger UI shows
/// an "Authorize" button, and applies it to every operation.
/// </summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = JwtBearerDefaults.AuthenticationScheme;

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste the access token from POST /api/auth/login (without the 'Bearer ' prefix).",
        };

        var requirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, document)] = [],
        };

        foreach (var operation in document.Paths.Values.SelectMany(p => p.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>()))
        {
            operation.Security ??= [];
            operation.Security.Add(requirement);
        }

        return Task.CompletedTask;
    }
}
