using Api.Authentication;
using Api.Extensions;
using Api.Middleware;
using Api.RateLimiting;
using BusinessLogic.Interfaces;
using BusinessLogic.Options;
using BusinessLogic.Services;
using DataAccess;
using DataAccess.Entities;
using DataAccess.Interfaces;
using DataAccess.Repositories;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Load variables from the .env file (searching parent directories) without
// overriding variables already set in the real environment.
Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.UsePlaceholderConfigForOpenApiBuild();
builder.EnsureLocalDatabase();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();

builder.Services.AddAppOpenApi();

// Data access
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
builder.Services.AddScoped<IFridgeRepository, FridgeRepository>();
builder.Services.AddScoped<IProductCatalogRepository, ProductCatalogRepository>();
builder.Services.AddScoped<IContainerRepository, ContainerRepository>();

// Business logic
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IFridgeLimitDefaults, FridgeLimitDefaults>();
builder.Services.AddScoped<IFridgeService, FridgeService>();
builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();
builder.Services.AddScoped<IContainerService, ContainerService>();

// Google / Apple sign-in. Singleton so the providers' signing keys stay cached.
builder.Services.AddOptions<ExternalAuthOptions>()
    .Bind(builder.Configuration.GetSection(ExternalAuthOptions.SectionName));
builder.Services.AddSingleton<IExternalTokenValidator, OidcExternalTokenValidator>();

// JWT authentication
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

builder.Services.ConfigureOptions<ConfigureJwtBearerOptions>();
builder.Services.AddScoped<AppJwtBearerEvents>();

builder.Services.AddAuthorization();

// Rate limiting
builder.Services.AddOptions<AuthRateLimitOptions>()
    .Bind(builder.Configuration.GetSection(AuthRateLimitOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<InviteCodeRateLimitOptions>()
    .Bind(builder.Configuration.GetSection(InviteCodeRateLimitOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddRateLimiter(_ => { });
builder.Services.ConfigureOptions<ConfigureRateLimiterOptions>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "FridgeFriend API");
        options.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();
// After authentication, so a policy can count per user. Valid tokens get their DB checks before a 429.
app.UseRateLimiter();

app.MapGet("/", () => "Hello World!")
    .ExcludeFromDescription();

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program;
