using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatchPony.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProtectAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE FUNCTION patchpony.prevent_audit_event_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RAISE EXCEPTION 'Audit events are append-only.';
                END;
                $$;
                
                CREATE TRIGGER audit_events_append_only
                BEFORE UPDATE OR DELETE ON patchpony.audit_events
                FOR EACH ROW
                EXECUTE FUNCTION patchpony.prevent_audit_event_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER audit_events_append_only ON patchpony.audit_events;
                DROP FUNCTION patchpony.prevent_audit_event_mutation();
                """);
        }
    }
}
