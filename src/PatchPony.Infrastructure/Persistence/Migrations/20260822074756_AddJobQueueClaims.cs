using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatchPony.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobQueueClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClaimExpiresAt",
                schema: "patchpony",
                table: "job_queue",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClaimId",
                schema: "patchpony",
                table: "job_queue",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimedBy",
                schema: "patchpony",
                table: "job_queue",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_queue_AvailableAt_ClaimExpiresAt_EnqueuedAt",
                schema: "patchpony",
                table: "job_queue",
                columns: new[] { "AvailableAt", "ClaimExpiresAt", "EnqueuedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_job_queue_AvailableAt_ClaimExpiresAt_EnqueuedAt",
                schema: "patchpony",
                table: "job_queue");

            migrationBuilder.DropColumn(
                name: "ClaimExpiresAt",
                schema: "patchpony",
                table: "job_queue");

            migrationBuilder.DropColumn(
                name: "ClaimId",
                schema: "patchpony",
                table: "job_queue");

            migrationBuilder.DropColumn(
                name: "ClaimedBy",
                schema: "patchpony",
                table: "job_queue");
        }
    }
}
