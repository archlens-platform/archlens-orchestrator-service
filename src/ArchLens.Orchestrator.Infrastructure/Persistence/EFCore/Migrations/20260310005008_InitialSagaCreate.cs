using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchLens.Orchestrator.Infrastructure.Persistence.EFCore.Migrations
{

    public partial class InitialSagaCreate : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "saga_states",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentState = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiagramId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResultJson = table.Column<string>(type: "text", nullable: true),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessingTimeMs = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saga_states", x => x.CorrelationId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_saga_states_AnalysisId",
                table: "saga_states",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_saga_states_CurrentState",
                table: "saga_states",
                column: "CurrentState");

            migrationBuilder.CreateIndex(
                name: "IX_saga_states_DiagramId",
                table: "saga_states",
                column: "DiagramId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saga_states");
        }
    }
}
