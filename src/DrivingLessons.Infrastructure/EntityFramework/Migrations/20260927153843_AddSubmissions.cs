using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrivingLessons.Infrastructure.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    publication_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    week_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_count = table.Column<int>(type: "integer", nullable: false),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    revised_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "slot_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_type = table.Column<int>(type: "integer", nullable: false),
                    constraint_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_slot_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_slot_requests_submissions_submission_id",
                        column: x => x.submission_id,
                        principalTable: "submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_slot_requests_submission_id",
                table: "slot_requests",
                column: "submission_id");

            migrationBuilder.CreateIndex(
                name: "IX_submissions_publication_id_student_id",
                table: "submissions",
                columns: new[] { "publication_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_submissions_week_schedule_id",
                table: "submissions",
                column: "week_schedule_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "slot_requests");

            migrationBuilder.DropTable(
                name: "submissions");
        }
    }
}
