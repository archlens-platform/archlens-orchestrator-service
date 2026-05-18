using ArchLens.Orchestrator.Infrastructure.Saga;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchLens.Orchestrator.Infrastructure.Persistence.EFCore.Configurations.SagaStateConfigurations;

public sealed class AnalysisSagaStateConfiguration : IEntityTypeConfiguration<AnalysisSagaState>
{
    public void Configure(EntityTypeBuilder<AnalysisSagaState> builder)
    {
        builder.ToTable("saga_states");

        builder.HasKey(x => x.CorrelationId);

        builder.Property(x => x.CurrentState)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.AnalysisId);
        builder.Property(x => x.DiagramId);

        builder.Property(x => x.FileName)
            .HasMaxLength(255);

        builder.Property(x => x.FileHash)
            .HasMaxLength(64);

        builder.Property(x => x.StoragePath)
            .HasMaxLength(500);

        builder.Property(x => x.UserId)
            .HasMaxLength(128);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.DiagramId);
        builder.HasIndex(x => x.AnalysisId);
        builder.HasIndex(x => x.CurrentState);
    }
}
