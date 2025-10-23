using Prometheus;
using Application;
using System;
using Application.Utils;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors();

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
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.

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

app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());
//app.UseHttpsRedirection();




//app.UseAuthorization();

app.MapControllers();

app.MapMetrics("/metrics");
app.Run();
