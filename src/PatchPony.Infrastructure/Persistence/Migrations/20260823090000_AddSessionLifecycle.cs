using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatchPony.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PatchPonyDbContext))]
    [Migration("20260823090000_AddSessionLifecycle")]
    public partial class AddSessionLifecycle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureCode",
                schema: "patchpony",
                table: "sessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "patchpony",
                table: "sessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Created");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StatusChangedAt",
                schema: "patchpony",
                table: "sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_Status_ExpiresAt",
                schema: "patchpony",
                table: "sessions",
                columns: new[] { "Status", "ExpiresAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_sessions_Status_ExpiresAt", schema: "patchpony", table: "sessions");
            migrationBuilder.DropColumn(name: "FailureCode", schema: "patchpony", table: "sessions");
            migrationBuilder.DropColumn(name: "Status", schema: "patchpony", table: "sessions");
            migrationBuilder.DropColumn(name: "StatusChangedAt", schema: "patchpony", table: "sessions");
        }
    }
}