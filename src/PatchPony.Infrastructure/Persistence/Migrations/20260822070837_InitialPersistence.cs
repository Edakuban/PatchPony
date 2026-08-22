using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatchPony.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "patchpony");

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "patchpony",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "jobs",
                schema: "patchpony",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_jobs_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "patchpony",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "repositories",
                schema: "patchpony",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RemoteUri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DefaultBranch = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repositories", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_repositories_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "patchpony",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "approvals",
                schema: "patchpony",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_approvals_jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "patchpony",
                        principalTable: "jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artifact_references",
                schema: "patchpony",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_artifact_references", x => x.Id);
                    table.ForeignKey(
                        name: "FK_artifact_references_jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "patchpony",
                        principalTable: "jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_events",
                schema: "patchpony",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audit_events_jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "patchpony",
                        principalTable: "jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_audit_events_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "patchpony",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_records",
                schema: "patchpony",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_records", x => x.Key);
                    table.ForeignKey(
                        name: "FK_idempotency_records_jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "patchpony",
                        principalTable: "jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_idempotency_records_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "patchpony",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "job_status_changes",
                schema: "patchpony",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    From = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    To = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_status_changes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_status_changes_jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "patchpony",
                        principalTable: "jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                schema: "patchpony",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sessions_jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "patchpony",
                        principalTable: "jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sessions_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "patchpony",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tool_invocations",
                schema: "patchpony",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RedactedParametersJson = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tool_invocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tool_invocations_jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "patchpony",
                        principalTable: "jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_approvals_JobId_RequestedAt",
                schema: "patchpony",
                table: "approvals",
                columns: new[] { "JobId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_artifact_references_JobId_CreatedAt",
                schema: "patchpony",
                table: "artifact_references",
                columns: new[] { "JobId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_JobId_OccurredAt",
                schema: "patchpony",
                table: "audit_events",
                columns: new[] { "JobId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_ProjectId_OccurredAt",
                schema: "patchpony",
                table: "audit_events",
                columns: new[] { "ProjectId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_records_ExpiresAt",
                schema: "patchpony",
                table: "idempotency_records",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_records_JobId",
                schema: "patchpony",
                table: "idempotency_records",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_records_ProjectId",
                schema: "patchpony",
                table: "idempotency_records",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_job_status_changes_JobId_OccurredAt",
                schema: "patchpony",
                table: "job_status_changes",
                columns: new[] { "JobId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_jobs_ProjectId_CreatedAt",
                schema: "patchpony",
                table: "jobs",
                columns: new[] { "ProjectId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_jobs_Status_CreatedAt",
                schema: "patchpony",
                table: "jobs",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_sessions_JobId_ExpiresAt",
                schema: "patchpony",
                table: "sessions",
                columns: new[] { "JobId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_sessions_ProjectId",
                schema: "patchpony",
                table: "sessions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_tool_invocations_JobId_StartedAt",
                schema: "patchpony",
                table: "tool_invocations",
                columns: new[] { "JobId", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approvals",
                schema: "patchpony");

            migrationBuilder.DropTable(
                name: "artifact_references",
                schema: "patchpony");

            migrationBuilder.DropTable(
                name: "audit_events",
                schema: "patchpony");

            migrationBuilder.DropTable(
                name: "idempotency_records",
                schema: "patchpony");

            migrationBuilder.DropTable(
                name: "job_status_changes",
                schema: "patchpony");

            migrationBuilder.DropTable(
                name: "repositories",
                schema: "patchpony");

            migrationBuilder.DropTable(
                name: "sessions",
                schema: "patchpony");

            migrationBuilder.DropTable(
                name: "tool_invocations",
                schema: "patchpony");

            migrationBuilder.DropTable(
                name: "jobs",
                schema: "patchpony");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "patchpony");
        }
    }
}
