var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "FridgeFriend API v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapGet("/", () => "Hello World!")
    .WithName("GetRoot");

app.Run();
