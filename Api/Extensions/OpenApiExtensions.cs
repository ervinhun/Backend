using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;

namespace Api.Extensions;

public static class OpenApiExtensions
{
    /// <summary>
    /// `dotnet build` runs the app's startup in this tool to write the OpenAPI document.
    /// It never serves a request, and has no secrets (CI has no .env).
    /// </summary>
    private static bool IsGeneratingDocument =>
        Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

    /// <summary>
    /// When the build is only writing the OpenAPI document, fills in the settings that startup
    /// validates (JWT secret, connection string), so it doesn't fail for lack of a .env.
    /// </summary>
    public static WebApplicationBuilder UsePlaceholderConfigForOpenApiBuild(this WebApplicationBuilder builder)
    {
        if (IsGeneratingDocument)
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = new('x', 64),
                ["ConnectionStrings:DefaultConnection"] = "Host=unused",
                ["LocalDatabase:AutoStart"] = "false",
            });
        }
        return builder;
    }

    /// <summary>
    /// The OpenAPI document the app's API client is generated from (openapi/FridgeFriend.json,
    /// written on every build). Served at /openapi/v1.json in Development.
    /// </summary>
    public static IServiceCollection AddAppOpenApi(this IServiceCollection services) =>
        services.AddOpenApi(options =>
        {
            options.AddOperationTransformer((operation, context, _) =>
            {
                // The generated client names its methods after this: AuthClient.login(...).
                if (context.Description.ActionDescriptor is ControllerActionDescriptor action)
                    operation.OperationId ??= action.ActionName;

                // The document requires a bearer token everywhere; these endpoints opt out of it.
                if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
                    operation.Security = [];

                return Task.CompletedTask;
            });

            options.AddDocumentTransformer((document, _, _) =>
            {
                // Operation ids must be unique, or two client methods clash. Name the action
                // more specifically (CreateItem rather than Create) when this fires.
                var duplicates = document.Paths.Values
                    .SelectMany(path => path.Operations?.Values.AsEnumerable() ?? [])
                    .GroupBy(operation => operation.OperationId)
                    .Where(group => group.Key is not null && group.Count() > 1)
                    .Select(group => group.Key)
                    .ToList();
                if (duplicates.Count > 0)
                    throw new InvalidOperationException(
                        $"Duplicate OpenAPI operation ids: {string.Join(", ", duplicates)}. Rename the controller actions.");
                return Task.CompletedTask;
            });

            // A Bearer scheme, so Swagger UI shows an "Authorize" button.
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                };
                document.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document)] = [],
                    },
                ];
                return Task.CompletedTask;
            });
        });
}
