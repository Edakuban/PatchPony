using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PatchPony.Infrastructure.Persistence;

public sealed class PatchPonyDbContextFactory : IDesignTimeDbContextFactory<PatchPonyDbContext>
{
    public PatchPonyDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PatchPony")
            ?? Environment.GetEnvironmentVariable("PATCHPONY_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Set ConnectionStrings__PatchPony or PATCHPONY_CONNECTION_STRING before running EF Core commands.");

        var options = new DbContextOptionsBuilder<PatchPonyDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PatchPonyDbContext(options);
    }
}
