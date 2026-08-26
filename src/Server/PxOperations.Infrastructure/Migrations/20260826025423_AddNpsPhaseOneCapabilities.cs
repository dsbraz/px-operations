using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PxOperations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNpsPhaseOneCapabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_nps_survey_responses_target_id",
                table: "nps_survey_responses");

            migrationBuilder.AddColumn<int>(
                name: "business_value",
                table: "nps_survey_responses",
                type: "integer",
                nullable: true);

            // B12 deixa em aberto o que fazer com disparo aberto antigo na virada.
            // Decisão: a régua vale para trás — expira em created_at + 20 dias.
            // Um link de três meses atrás já nasce vencido, que é a verdade
            // ("mandei e ninguém respondeu"), e o quadro oferece gerar novo.
            //
            // Entra anulável e só depois vira NOT NULL: o default do EF seria o
            // sentinela de ano 1, que marcaria TODO disparo como expirado.
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "nps_dispatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE nps_dispatches SET expires_at = created_at + interval '20 days' WHERE expires_at IS NULL;");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "expires_at",
                table: "nps_dispatches",
                type: "timestamp with time zone",
                nullable: false);

            migrationBuilder.CreateTable(
                name: "nps_collection_waivers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    dismissed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reactivated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nps_collection_waivers", x => x.id);
                    table.ForeignKey(
                        name: "FK_nps_collection_waivers_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_nps_survey_responses_target_id",
                table: "nps_survey_responses",
                column: "target_id",
                unique: true,
                filter: "contact_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_nps_collection_waivers_project_id",
                table: "nps_collection_waivers",
                column: "project_id",
                unique: true,
                filter: "reactivated_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nps_collection_waivers");

            migrationBuilder.DropIndex(
                name: "IX_nps_survey_responses_target_id",
                table: "nps_survey_responses");

            migrationBuilder.DropColumn(
                name: "business_value",
                table: "nps_survey_responses");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "nps_dispatches");

            migrationBuilder.CreateIndex(
                name: "IX_nps_survey_responses_target_id",
                table: "nps_survey_responses",
                column: "target_id",
                unique: true);
        }
    }
}
