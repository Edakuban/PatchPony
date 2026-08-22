using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatchPony.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "knowledge_sources",
                schema: "patchpony",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    RemoteUri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DefaultBranch = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_sources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_knowledge_sources_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "patchpony",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_sources_ProjectId_Name",
                schema: "patchpony",
                table: "knowledge_sources",
                columns: new[] { "ProjectId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "knowledge_sources",
                schema: "patchpony");
        }
    }
}
