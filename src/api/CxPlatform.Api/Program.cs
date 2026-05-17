using System.Text;
using System.Threading.RateLimiting;
using CxPlatform.Api.Auth;
using CxPlatform.Api.Middleware;
using CxPlatform.Domain.Enums;
using CxPlatform.Infrastructure.Channels;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ────────────────────────────────────────────────────────────
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

// ── DbContext (MySQL) ───────────────────────────────────────────────────────
var connStr = builder.Configuration.GetConnectionString("Default")
    ?? "server=localhost;port=3306;database=cx_platform;user=cx;password=cx;";
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 36))));

// ── Auth (JWT) ──────────────────────────────────────────────────────────────
builder.Services.AddSingleton<TokenService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = false; // dev — gateway terminates TLS in prod
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization(o =>
{
    foreach (var role in new[] { "admin", "supervisor", "agent", "quality", "customer", "executive" })
        o.AddPolicy($"role:{role}", p => p.RequireRole(role));
});

// ── CORS allow-list ─────────────────────────────────────────────────────────
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:4200", "http://localhost:5000", "http://localhost:5173")
     .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// ── Rate limiter — 60 req/min/IP on /api/* ──────────────────────────────────
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = 429;
    opts.AddPolicy("api", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 60,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            }));
});

// ── Body size cap (1MB) ─────────────────────────────────────────────────────
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 1024 * 1024);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 1024 * 1024);

// ── Channel adapters ────────────────────────────────────────────────────────
builder.Services.AddSingleton<IChannelAdapter, EmailAdapter>();
builder.Services.AddSingleton<IChannelAdapter, WhatsAppAdapter>();
builder.Services.AddSingleton<IChannelAdapter, ChatAdapter>();
builder.Services.AddSingleton<IChannelAdapterRegistry, ChannelAdapterRegistry>();

// ── Controllers + Swagger ───────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── Migrate + seed on startup ────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        await Seed.RunAsync(db);
    }
    catch (Exception ex)
    {
        var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        log.LogWarning(ex, "Skipping migrate/seed at startup — DB may not be reachable yet.");
    }
}

// ── Middleware pipeline ─────────────────────────────────────────────────────
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<AuditMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers().RequireRateLimiting("api");

app.Run();
