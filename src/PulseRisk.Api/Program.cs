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
builder.Services.AddOpenApi();

builder.Services
    .AddPulseRiskApplication()
    .AddPulseRiskInfrastructure(builder.Configuration)
    .AddPulseRiskBackgroundWorkers(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSerilogRequestLogging();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
