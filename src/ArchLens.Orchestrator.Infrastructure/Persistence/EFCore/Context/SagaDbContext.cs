using ArchLens.Orchestrator.Infrastructure.Saga;
using Microsoft.EntityFrameworkCore;

namespace ArchLens.Orchestrator.Infrastructure.Persistence.EFCore.Context;

public sealed class SagaDbContext : DbContext
{
    public DbSet<AnalysisSagaState> SagaStates => Set<AnalysisSagaState>();

    public SagaDbContext(DbContextOptions<SagaDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SagaDbContext).Assembly);
    }
}
