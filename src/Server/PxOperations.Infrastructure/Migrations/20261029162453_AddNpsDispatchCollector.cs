using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PxOperations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNpsDispatchCollector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "collector_name",
                table: "nps_dispatches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "collector_name",
                table: "nps_dispatches");
        }
    }
}
