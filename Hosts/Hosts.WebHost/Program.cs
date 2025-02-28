using BSDigital.MetaExchange.Core.Api.AspNetCore;
using BSDigital.MetaExchange.Core.Db.EfCore.SQLite;
using BSDigital.MetaExchange.Core.UseCases;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSqLiteDb(builder.Configuration.GetRequiredSection("SqLiteDb").Bind);
builder.Services.AddUseCases();
builder.Services.AddAspNetCoreApi();

// Add Swagger Services
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MetaExchange API",
        Version = "v1",
        Description = "API for the BSDigital MetaExchange"
    });
});

var app = builder.Build();

app.Services.EnsureDatabaseCreated();

// Enable Swagger in Development Mode
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MetaExchange API v1");
    });
}

app.MapAspNetCoreApi();

app.Run();