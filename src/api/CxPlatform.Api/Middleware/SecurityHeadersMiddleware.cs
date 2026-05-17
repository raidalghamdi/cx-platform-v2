namespace CxPlatform.Api.Middleware;

// Same hardening rules the React pilot applies on the Express server, ported
// to .NET. CSP is permissive enough for the YARP gateway to serve the SPA
// from the same origin.
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _isProduction;

    public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment env)
    {
        _next = next;
        _isProduction = env.IsProduction();
    }

    public async Task Invoke(HttpContext ctx)
    {
        var h = ctx.Response.Headers;
        h["X-Frame-Options"] = "DENY";
        h["X-Content-Type-Options"] = "nosniff";
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";
        if (_isProduction)
            h["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        // Modest CSP — allow Google Fonts (used by the SPA's Exo 2 import).
        h["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com data:; " +
            "img-src 'self' data: blob:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'; " +
            "object-src 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self';";
        ctx.Response.Headers.Remove("Server");
        ctx.Response.Headers.Remove("X-Powered-By");
        await _next(ctx);
    }
}
