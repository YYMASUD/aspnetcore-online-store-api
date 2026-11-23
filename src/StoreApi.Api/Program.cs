var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();

/// <summary>Entry point marker used by integration tests via <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
