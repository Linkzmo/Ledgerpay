using Messaging;
using Microsoft.EntityFrameworkCore;
using Notifications.Worker.HostedServices;
using Notifications.Worker.Infrastructure;
using Observability.Configuration;

var builder = Host.CreateApplicationBuilder(args);

ObservabilityExtensions.ConfigureSerilog(builder, "notifications-worker");

builder.Services.AddDbContext<NotificationsDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "notifications"));
});

builder.Services.AddMessaging(builder.Configuration);
builder.Services.AddLedgerpayOpenTelemetry(builder.Configuration, "notifications-worker", includeAspNetInstrumentation: false);

builder.Services.AddHostedService<NotificationsConsumer>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    dbContext.Database.EnsureCreated();
}

host.Run();
