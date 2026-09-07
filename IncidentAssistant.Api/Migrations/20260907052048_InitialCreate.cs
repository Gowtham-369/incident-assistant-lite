using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IncidentAssistant.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Incidents",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "ResolutionNotes", "ResolvedAt", "Severity", "Status", "Title" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Performance", new DateTime(2026, 9, 1, 14, 30, 0, 0, DateTimeKind.Utc), "p99 latency jumped from 200ms to 4s after the 14:00 deploy. Errors in logs mention connection pool exhaustion.", null, null, "High", "Open", "API latency spike on checkout service" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Email", new DateTime(2026, 9, 3, 9, 15, 0, 0, DateTimeKind.Utc), "Password reset emails not being delivered. SMTP relay returning 451 errors intermittently.", null, null, "Medium", "InProgress", "Users unable to reset passwords" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Data", new DateTime(2026, 8, 28, 2, 0, 0, 0, DateTimeKind.Utc), "Data warehouse sync failed at step 3 with a timeout. Downstream dashboards showing stale data.", "Increased timeout and added retry; root cause was a long-running lock from a manual query.", new DateTime(2026, 8, 28, 8, 45, 0, 0, DateTimeKind.Utc), "Low", "Resolved", "Nightly ETL job failed" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Incidents");
        }
    }
}
