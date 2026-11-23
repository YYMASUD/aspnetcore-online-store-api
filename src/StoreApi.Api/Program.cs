using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StoreApi.Api.Extensions;
using StoreApi.Api.Filters;
using StoreApi.Api.Middleware;
using StoreApi.Application;
using StoreApi.Application.Common.Models;
using StoreApi.Infrastructure;
using StoreApi.Infrastructure.Identity;
using StoreApi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddFrontendCors();
builder.Services.AddAuthRateLimiting();
builder.Services.AddSwaggerDocumentation();

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<StoreDbContext>("database");

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Online Store API v1");
        options.DisplayRequestDuration();
    });
}

await app.InitializeDatabaseAsync();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

/// <summary>Entry point marker used by integration tests via <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
