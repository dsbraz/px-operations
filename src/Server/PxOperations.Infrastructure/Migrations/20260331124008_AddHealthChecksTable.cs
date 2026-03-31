using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PxOperations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthChecksTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "health_checks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    sub_project = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    week = table.Column<DateOnly>(type: "date", nullable: false),
                    reporter_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    practices_count = table.Column<int>(type: "integer", nullable: false),
                    scope = table.Column<int>(type: "integer", nullable: false),
                    schedule = table.Column<int>(type: "integer", nullable: false),
                    quality = table.Column<int>(type: "integer", nullable: false),
                    satisfaction = table.Column<int>(type: "integer", nullable: false),
                    expansion_opportunity = table.Column<bool>(type: "boolean", nullable: false),
                    expansion_comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    action_plan_needed = table.Column<bool>(type: "boolean", nullable: false),
                    highlights = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_health_checks", x => x.id);
                    table.ForeignKey(
                        name: "FK_health_checks_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_health_checks_project_id_week",
                table: "health_checks",
                columns: new[] { "project_id", "week" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "health_checks");
        }
    }
}
