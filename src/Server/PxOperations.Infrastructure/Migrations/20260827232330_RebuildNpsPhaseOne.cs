using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PxOperations.Infrastructure.Migrations;

public partial class RebuildNpsPhaseOne : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_nps_dispatch_targets_projects_project_id",
            table: "nps_dispatch_targets");
        migrationBuilder.DropForeignKey(
            name: "FK_nps_dispatches_projects_project_id",
            table: "nps_dispatches");
        migrationBuilder.DropIndex(
            name: "IX_nps_survey_responses_target_id",
            table: "nps_survey_responses");
        migrationBuilder.DropIndex(
            name: "IX_nps_dispatches_project_id_status",
            table: "nps_dispatches");
        migrationBuilder.DropIndex(
            name: "IX_nps_dispatch_targets_project_id",
            table: "nps_dispatch_targets");

        migrationBuilder.CreateTable(
            name: "nps_collections",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                project_id = table.Column<int>(type: "integer", nullable: false),
                waiver_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                waived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_nps_collections", x => x.id);
                table.ForeignKey(
                    name: "FK_nps_collections_projects_project_id",
                    column: x => x.project_id,
                    principalTable: "projects",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(
            name: "IX_nps_collections_project_id",
            table: "nps_collections",
            column: "project_id",
            unique: true);
        migrationBuilder.Sql("""
            INSERT INTO nps_collections (project_id)
            SELECT id FROM projects
            ORDER BY id;
            """);

        migrationBuilder.AddColumn<int>(
            name: "collection_id",
            table: "nps_dispatches",
            type: "integer",
            nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "expires_at",
            table: "nps_dispatches",
            type: "timestamp with time zone",
            nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "format",
            table: "nps_survey_responses",
            type: "integer",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "normalized_respondent_email",
            table: "nps_survey_responses",
            type: "character varying(320)",
            maxLength: 320,
            nullable: true);
        migrationBuilder.RenameColumn(
            name: "scope",
            table: "nps_survey_responses",
            newName: "business_value");

        migrationBuilder.Sql("""
            UPDATE nps_dispatches AS dispatch
            SET collection_id = collection.id,
                expires_at = dispatch.created_at + INTERVAL '20 days'
            FROM nps_collections AS collection
            WHERE collection.project_id = dispatch.project_id;

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM nps_dispatches
                    WHERE collection_id IS NULL OR expires_at IS NULL
                ) THEN
                    RAISE EXCEPTION 'NPS dispatch collection linkage validation failed';
                END IF;
            END $$;

            WITH ordered_open_dispatches AS (
                SELECT id,
                       LEAD(created_at) OVER (
                           PARTITION BY collection_id, format
                           ORDER BY created_at, id
                       ) AS next_created_at
                FROM nps_dispatches
                WHERE status = 0
            )
            UPDATE nps_dispatches AS dispatch
            SET status = 1,
                closed_at = ordered.next_created_at
            FROM ordered_open_dispatches AS ordered
            WHERE dispatch.id = ordered.id
              AND ordered.next_created_at IS NOT NULL;

            UPDATE nps_survey_responses AS response
            SET format = dispatch.format,
                normalized_respondent_email = NULLIF(LOWER(BTRIM(response.respondent_email)), ''),
                score = CASE WHEN response.score = 0 THEN 1 ELSE response.score END,
                quality = CASE
                    WHEN response.quality IS NULL THEN NULL
                    ELSE GREATEST(1, LEAST(5, CEIL(response.quality / 2.0)::integer))
                END,
                schedule = CASE
                    WHEN response.schedule IS NULL THEN NULL
                    ELSE GREATEST(1, LEAST(5, CEIL(response.schedule / 2.0)::integer))
                END,
                communication = CASE
                    WHEN response.communication IS NULL THEN NULL
                    ELSE GREATEST(1, LEAST(5, CEIL(response.communication / 2.0)::integer))
                END,
                business_value = CASE
                    WHEN response.business_value IS NULL THEN NULL
                    ELSE GREATEST(1, LEAST(5, CEIL(response.business_value / 2.0)::integer))
                END
            FROM nps_dispatches AS dispatch
            WHERE dispatch.id = response.dispatch_id;

            UPDATE nps_survey_responses
            SET classification = CASE
                WHEN score <= 6 THEN 0
                WHEN score <= 8 THEN 1
                ELSE 2
            END;

            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM nps_survey_responses WHERE format IS NULL) THEN
                    RAISE EXCEPTION 'NPS response format backfill validation failed';
                END IF;
            END $$;
            """);

        migrationBuilder.AlterColumn<int>(
            name: "collection_id",
            table: "nps_dispatches",
            type: "integer",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);
        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "expires_at",
            table: "nps_dispatches",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "timestamp with time zone",
            oldNullable: true);
        migrationBuilder.AlterColumn<int>(
            name: "format",
            table: "nps_survey_responses",
            type: "integer",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);

        migrationBuilder.DropColumn(name: "tags", table: "nps_survey_responses");
        migrationBuilder.DropColumn(name: "created_by", table: "nps_dispatches");
        migrationBuilder.DropColumn(name: "period_end", table: "nps_dispatches");
        migrationBuilder.DropColumn(name: "period_start", table: "nps_dispatches");
        migrationBuilder.DropColumn(name: "project_id", table: "nps_dispatches");
        migrationBuilder.DropColumn(name: "project_id", table: "nps_dispatch_targets");

        migrationBuilder.CreateIndex(
            name: "IX_nps_dispatches_collection_id_format",
            table: "nps_dispatches",
            columns: new[] { "collection_id", "format" },
            unique: true,
            filter: "status = 0");
        migrationBuilder.CreateIndex(
            name: "IX_nps_survey_responses_target_id",
            table: "nps_survey_responses",
            column: "target_id",
            unique: true,
            filter: "contact_id IS NOT NULL");
        migrationBuilder.CreateIndex(
            name: "IX_nps_survey_responses_target_id_normalized_respondent_email",
            table: "nps_survey_responses",
            columns: new[] { "target_id", "normalized_respondent_email" },
            unique: true,
            filter: "contact_id IS NULL AND normalized_respondent_email IS NOT NULL");

        migrationBuilder.AddCheckConstraint(
            name: "CK_nps_survey_responses_score",
            table: "nps_survey_responses",
            sql: "score BETWEEN 1 AND 10");
        migrationBuilder.AddCheckConstraint(
            name: "CK_nps_survey_responses_quality",
            table: "nps_survey_responses",
            sql: "quality IS NULL OR quality BETWEEN 1 AND 5");
        migrationBuilder.AddCheckConstraint(
            name: "CK_nps_survey_responses_schedule",
            table: "nps_survey_responses",
            sql: "schedule IS NULL OR schedule BETWEEN 1 AND 5");
        migrationBuilder.AddCheckConstraint(
            name: "CK_nps_survey_responses_communication",
            table: "nps_survey_responses",
            sql: "communication IS NULL OR communication BETWEEN 1 AND 5");
        migrationBuilder.AddCheckConstraint(
            name: "CK_nps_survey_responses_business_value",
            table: "nps_survey_responses",
            sql: "business_value IS NULL OR business_value BETWEEN 1 AND 5");

        migrationBuilder.AddForeignKey(
            name: "FK_nps_dispatches_nps_collections_collection_id",
            table: "nps_dispatches",
            column: "collection_id",
            principalTable: "nps_collections",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => throw new NotSupportedException("The NPS Phase 1 data conversion is not semantically reversible.");
}
