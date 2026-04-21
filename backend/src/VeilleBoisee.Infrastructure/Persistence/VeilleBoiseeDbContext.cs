using Microsoft.EntityFrameworkCore;
using VeilleBoisee.Domain.Entities;

namespace VeilleBoisee.Infrastructure.Persistence;

public sealed class VeilleBoiseeDbContext : DbContext
{
    public VeilleBoiseeDbContext(DbContextOptions<VeilleBoiseeDbContext> options) : base(options) { }

    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VeilleBoiseeDbContext).Assembly);
    }
}
