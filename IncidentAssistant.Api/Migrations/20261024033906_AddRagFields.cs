using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentAssistant.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRagFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedGroups",
                table: "Incidents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[\"oncall-all\"]");

            migrationBuilder.AddColumn<string>(
                name: "ErrorSignatures",
                table: "Incidents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "RootCause",
                table: "Incidents",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Service",
                table: "Incidents",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "unknown");

            migrationBuilder.UpdateData(
                table: "Incidents",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "AllowedGroups", "ErrorSignatures", "RootCause", "Service" },
                values: new object[] { "[\"oncall-all\"]", "[]", null, "checkout-api" });

            migrationBuilder.UpdateData(
                table: "Incidents",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "AllowedGroups", "ErrorSignatures", "RootCause", "Service" },
                values: new object[] { "[\"oncall-all\"]", "[]", null, "notification-service" });

            migrationBuilder.UpdateData(
                table: "Incidents",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "AllowedGroups", "ErrorSignatures", "RootCause", "Service" },
                values: new object[] { "[\"oncall-all\"]", "[]", null, "data-pipeline" });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_Service",
                table: "Incidents",
                column: "Service");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Incidents_Service",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "AllowedGroups",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ErrorSignatures",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "RootCause",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "Service",
                table: "Incidents");
        }
    }
}
