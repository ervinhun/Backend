using DotNetEnv;

// Load variables from the .env file (searching parent directories) without
// overriding variables already set in the real environment.
Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "FridgeFriend API");
        options.RoutePrefix = "swagger";
    });
}

app.MapGet("/", () => "Hello World!")
    .WithName("GetRoot");

app.Run();
