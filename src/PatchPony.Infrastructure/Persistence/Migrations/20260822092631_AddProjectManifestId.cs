using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatchPony.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectManifestId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ManifestId",
                schema: "patchpony",
                table: "projects",
                type: "character varying(63)",
                maxLength: 63,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE patchpony.projects
                SET "ManifestId" = 'legacy-' || replace("Id"::text, '-', '')
                WHERE "ManifestId" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ManifestId",
                schema: "patchpony",
                table: "projects",
                type: "character varying(63)",
                maxLength: 63,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(63)",
                oldMaxLength: 63,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_projects_ManifestId",
                schema: "patchpony",
                table: "projects",
                column: "ManifestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_projects_ManifestId",
                schema: "patchpony",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "ManifestId",
                schema: "patchpony",
                table: "projects");
        }
    }
}