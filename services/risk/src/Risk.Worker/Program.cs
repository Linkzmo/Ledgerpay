using Messaging;
using Microsoft.EntityFrameworkCore;
using Observability.Configuration;
using Risk.Worker.HostedServices;
using Risk.Worker.Infrastructure;
using Risk.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

ObservabilityExtensions.ConfigureSerilog(builder, "risk-worker");

builder.Services.AddDbContext<RiskDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "risk"));
});

builder.Services.AddMessaging(builder.Configuration);
builder.Services.AddLedgerpayOpenTelemetry(builder.Configuration, "risk-worker", includeAspNetInstrumentation: false);
builder.Services.AddScoped<RiskDecisionService>();

builder.Services.AddHostedService<RiskOutboxPublisher>();
builder.Services.AddHostedService<RiskPaymentCreatedConsumer>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RiskDbContext>();
    dbContext.Database.EnsureCreated();
}

host.Run();
