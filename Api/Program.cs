using Api.Extensions;
using Api.Middleware;
using DotNetEnv;

// Load variables from the .env file (searching parent directories) without
// overriding variables already set in the real environment.
Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.EnsureLocalDatabase();

builder.Services.AddControllers();
builder.Services.AddOpenApiWithJwt();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();

builder.Services.AddDataAccess(builder.Configuration);
builder.Services.AddBusinessLogic();
builder.Services.AddJwtAuthentication(builder.Configuration);

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

app.MapGet("/", () => "Hello World!")
    .WithName("GetRoot");

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program;
