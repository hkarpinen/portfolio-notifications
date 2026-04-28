using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notifications");

            migrationBuilder.CreateTable(
                name: "comment_author_projections",
                schema: "notifications",
                columns: table => new
                {
                    comment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comment_author_projections", x => x.comment_id);
                });

            migrationBuilder.CreateTable(
                name: "household_member_projections",
                schema: "notifications",
                columns: table => new
                {
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_household_member_projections", x => new { x.household_id, x.user_id });
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "processed_events",
                schema: "notifications",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_events", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "thread_author_projections",
                schema: "notifications",
                columns: table => new
                {
                    thread_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_thread_author_projections", x => x.thread_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_created_at",
                schema: "notifications",
                table: "notifications",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_read",
                schema: "notifications",
                table: "notifications",
                columns: new[] { "user_id", "read" });

            migrationBuilder.CreateIndex(
                name: "ix_processed_events_processed_at",
                schema: "notifications",
                table: "processed_events",
                column: "processed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comment_author_projections",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "household_member_projections",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "processed_events",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "thread_author_projections",
                schema: "notifications");
        }
    }
}
