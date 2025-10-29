using Prometheus;
using Application;
using System;
using Application.Utils;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddCors();

    builder.Services.AddMemoryCache();

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "PersonTree API",
            Version = "v1"
        });
    });

    builder.Services.AddApplicationServices(builder.Configuration);

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<UserDb>();
        Log.Information("Applying database migrations...");
        dbContext.Database.Migrate();
        Log.Information("Database migrations applied successfully");
    }

    app.UseSwagger(c =>
    {
        c.RouteTemplate = "api/swagger/{documentName}/swagger.json";
    });

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/api/swagger/v1/swagger.json", "PersonTree API V1");
        c.RoutePrefix = "api/swagger";
    });

    app.MapWhen(context => context.Request.Path == "/api/", appBuilder =>
    {
        appBuilder.Run(async context =>
        {
            context.Response.Redirect("/api/swagger");
            await Task.CompletedTask;
        });
    });

    app.UseRouting();
    app.UseHttpMetrics();

    // Global exception handling middleware
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception occurred");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = "An internal server error occurred" });
        }
    });

    app.UseCors(builder => builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());

    app.MapControllers();
    app.MapMetrics("/metrics");

    Log.Information("Application started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}