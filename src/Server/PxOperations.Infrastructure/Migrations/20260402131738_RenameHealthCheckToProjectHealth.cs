using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PxOperations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameHealthCheckToProjectHealth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "health_checks",
                newName: "project_health");

            migrationBuilder.RenameIndex(
                name: "IX_health_checks_project_id_week",
                table: "project_health",
                newName: "IX_project_health_project_id_week");

            migrationBuilder.Sql(
                """ALTER TABLE project_health RENAME CONSTRAINT "PK_health_checks" TO "PK_project_health";""");

            migrationBuilder.Sql(
                """ALTER TABLE project_health RENAME CONSTRAINT "FK_health_checks_projects_project_id" TO "FK_project_health_projects_project_id";""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "project_health",
                newName: "health_checks");

            migrationBuilder.RenameIndex(
                name: "IX_project_health_project_id_week",
                table: "health_checks",
                newName: "IX_health_checks_project_id_week");

            migrationBuilder.Sql(
                """ALTER TABLE health_checks RENAME CONSTRAINT "PK_project_health" TO "PK_health_checks";""");

            migrationBuilder.Sql(
                """ALTER TABLE health_checks RENAME CONSTRAINT "FK_project_health_projects_project_id" TO "FK_health_checks_projects_project_id";""");
        }
    }
}
