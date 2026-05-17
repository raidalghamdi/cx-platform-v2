// YARP gateway — single ingress on http://localhost:5000
//   /api/*  → http://localhost:5001  (API)
//   /*      → http://localhost:4200  (Angular dev server in dev)
//
// In production we'd swap the `web` cluster to serve the static Angular
// bundle from disk via a static-file middleware, but the route shape stays.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Security headers — defence in depth even though API also carries them.
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Frame-Options"] = "DENY";
    h["X-Content-Type-Options"] = "nosniff";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    if (app.Environment.IsProduction())
        h["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    h["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com data:; " +
        "img-src 'self' data: blob:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "object-src 'none'; " +
        "base-uri 'self';";
    ctx.Response.Headers.Remove("Server");
    await next();
});

app.MapReverseProxy();

app.Run();
