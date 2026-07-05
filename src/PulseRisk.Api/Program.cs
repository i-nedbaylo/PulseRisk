using PulseRisk.Api.ErrorHandling;
using PulseRisk.Api.OpenApi;
using PulseRisk.Api.Startup;
using PulseRisk.Application;
using PulseRisk.BackgroundWorkers;
using PulseRisk.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddControllers();
builder.Services.AddPulseRiskSwagger();

builder.Services
    .AddPulseRiskApplication()
    .AddPulseRiskInfrastructure(builder.Configuration)
    .AddPulseRiskBackgroundWorkers(builder.Configuration);

var app = builder.Build();

await app.ApplyPulseRiskDatabaseMigrationsAsync();

if (app.Environment.IsDevelopment())
{
    app.UsePulseRiskSwagger();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
