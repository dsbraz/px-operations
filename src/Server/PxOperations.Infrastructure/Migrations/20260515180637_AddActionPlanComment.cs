using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PxOperations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActionPlanComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "action_plan_comment",
                table: "project_health",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "action_plan_comment",
                table: "project_health");
        }
    }
}
