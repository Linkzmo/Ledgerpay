using System.Text;
using Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Observability.Configuration;
using Serilog;
using Ledger.Api.HostedServices;
using Ledger.Api.Infrastructure;
using Ledger.Api.Security;

var builder = WebApplication.CreateBuilder(args);

ObservabilityExtensions.ConfigureSerilog(builder, "ledger-api");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ledgerpay Ledger API",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    options.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [securityScheme] = Array.Empty<string>()
    });
});

builder.Services.AddDbContext<LedgerDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "ledger"));
});

builder.Services.AddMessaging(builder.Configuration);
builder.Services.AddLedgerpayOpenTelemetry(builder.Configuration, "ledger-api", includeAspNetInstrumentation: true);

builder.Services.AddHostedService<LedgerOutboxPublisher>();
builder.Services.AddHostedService<LedgerEventsConsumer>();

var signingKey = builder.Configuration["Authentication:SigningKey"] ?? "ledgerpay-super-secret-signing-key-change-me";
var issuer = builder.Configuration["Authentication:Issuer"] ?? "ledgerpay.local";
var audience = builder.Configuration["Authentication:Audience"] ?? "ledgerpay.api";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
        };
    });

builder.Services.AddAuthorization(AuthorizationPolicies.AddLedgerPolicies);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseSerilogRequestLogging();
app.UseCorrelationId();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "ledger-api" }));

app.Run();
