using System;
using ArchLens.Orchestrator.Infrastructure.Persistence.EFCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArchLens.Orchestrator.Infrastructure.Persistence.EFCore.Migrations
{
    [DbContext(typeof(SagaDbContext))]
    [Migration("20260310005008_InitialSagaCreate")]
    partial class InitialSagaCreate
    {

        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ArchLens.Orchestrator.Infrastructure.Saga.AnalysisSagaState", b =>
                {
                    b.Property<Guid>("CorrelationId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("AnalysisId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("CurrentState")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<Guid>("DiagramId")
                        .HasColumnType("uuid");

                    b.Property<string>("ErrorMessage")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<string>("FileHash")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("FileName")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<long?>("ProcessingTimeMs")
                        .HasColumnType("bigint");

                    b.Property<Guid?>("ReportId")
                        .HasColumnType("uuid");

                    b.Property<string>("ResultJson")
                        .HasColumnType("text");

                    b.Property<int>("RetryCount")
                        .HasColumnType("integer");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("bytea");

                    b.Property<string>("StoragePath")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("UserId")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.HasKey("CorrelationId");

                    b.HasIndex("AnalysisId");

                    b.HasIndex("CurrentState");

                    b.HasIndex("DiagramId");

                    b.ToTable("saga_states", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
