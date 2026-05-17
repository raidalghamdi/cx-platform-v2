using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CxPlatform.Infrastructure.Persistence;

// Used by `dotnet ef` tooling so migrations can be added without running the
// real Program.cs. Uses a placeholder MySQL server version — not at runtime.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        var connStr = Environment.GetEnvironmentVariable("CX_CONN_STR")
            ?? "server=localhost;port=3306;database=cx_platform;user=cx;password=cx;";
        builder.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 36)));
        return new AppDbContext(builder.Options);
    }
}
