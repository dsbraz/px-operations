using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PxOperations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNpsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "nps_contacts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    role = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nps_contacts", x => x.id);
                    table.ForeignKey(
                        name: "FK_nps_contacts_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nps_dispatches",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    format = table.Column<int>(type: "integer", nullable: false),
                    language = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nps_dispatches", x => x.id);
                    table.ForeignKey(
                        name: "FK_nps_dispatches_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nps_dispatch_targets",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    dispatch_id = table.Column<int>(type: "integer", nullable: false),
                    contact_id = table.Column<int>(type: "integer", nullable: true),
                    token = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nps_dispatch_targets", x => x.id);
                    table.ForeignKey(
                        name: "FK_nps_dispatch_targets_nps_contacts_contact_id",
                        column: x => x.contact_id,
                        principalTable: "nps_contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_nps_dispatch_targets_nps_dispatches_dispatch_id",
                        column: x => x.dispatch_id,
                        principalTable: "nps_dispatches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_nps_dispatch_targets_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nps_survey_responses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    dispatch_id = table.Column<int>(type: "integer", nullable: false),
                    target_id = table.Column<int>(type: "integer", nullable: false),
                    contact_id = table.Column<int>(type: "integer", nullable: true),
                    score = table.Column<int>(type: "integer", nullable: false),
                    classification = table.Column<int>(type: "integer", nullable: false),
                    scope = table.Column<int>(type: "integer", nullable: true),
                    schedule = table.Column<int>(type: "integer", nullable: true),
                    quality = table.Column<int>(type: "integer", nullable: true),
                    communication = table.Column<int>(type: "integer", nullable: true),
                    tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    respondent_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    respondent_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nps_survey_responses", x => x.id);
                    table.ForeignKey(
                        name: "FK_nps_survey_responses_nps_contacts_contact_id",
                        column: x => x.contact_id,
                        principalTable: "nps_contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_nps_survey_responses_nps_dispatch_targets_target_id",
                        column: x => x.target_id,
                        principalTable: "nps_dispatch_targets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_nps_survey_responses_nps_dispatches_dispatch_id",
                        column: x => x.dispatch_id,
                        principalTable: "nps_dispatches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_nps_survey_responses_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_nps_contacts_project_id_email",
                table: "nps_contacts",
                columns: new[] { "project_id", "email" });

            migrationBuilder.CreateIndex(
                name: "IX_nps_dispatch_targets_contact_id",
                table: "nps_dispatch_targets",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "IX_nps_dispatch_targets_dispatch_id_contact_id",
                table: "nps_dispatch_targets",
                columns: new[] { "dispatch_id", "contact_id" },
                unique: true,
                filter: "contact_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_nps_dispatch_targets_project_id",
                table: "nps_dispatch_targets",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_nps_dispatch_targets_token",
                table: "nps_dispatch_targets",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nps_dispatches_project_id_status",
                table: "nps_dispatches",
                columns: new[] { "project_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_nps_survey_responses_contact_id",
                table: "nps_survey_responses",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "IX_nps_survey_responses_dispatch_id_submitted_at",
                table: "nps_survey_responses",
                columns: new[] { "dispatch_id", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_nps_survey_responses_project_id_submitted_at",
                table: "nps_survey_responses",
                columns: new[] { "project_id", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_nps_survey_responses_target_id",
                table: "nps_survey_responses",
                column: "target_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nps_survey_responses");

            migrationBuilder.DropTable(
                name: "nps_dispatch_targets");

            migrationBuilder.DropTable(
                name: "nps_contacts");

            migrationBuilder.DropTable(
                name: "nps_dispatches");
        }
    }
}
