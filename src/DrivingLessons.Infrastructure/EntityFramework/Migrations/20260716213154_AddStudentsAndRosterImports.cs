using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DrivingLessons.Infrastructure.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentsAndRosterImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roster_imports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    imported_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    added_count = table.Column<int>(type: "integer", nullable: false),
                    updated_count = table.Column<int>(type: "integer", nullable: false),
                    deactivated_count = table.Column<int>(type: "integer", nullable: false),
                    failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roster_imports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    national_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    car_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    license_type = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_students", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roster_import_entries",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    national_id = table.Column<string>(type: "text", nullable: false),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    roster_import_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roster_import_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_roster_import_entries_roster_imports_roster_import_id",
                        column: x => x.roster_import_id,
                        principalTable: "roster_imports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roster_import_failures",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    student_name = table.Column<string>(type: "text", nullable: true),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    roster_import_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roster_import_failures", x => x.id);
                    table.ForeignKey(
                        name: "FK_roster_import_failures_roster_imports_roster_import_id",
                        column: x => x.roster_import_id,
                        principalTable: "roster_imports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_roster_import_entries_roster_import_id",
                table: "roster_import_entries",
                column: "roster_import_id");

            migrationBuilder.CreateIndex(
                name: "IX_roster_import_failures_roster_import_id",
                table: "roster_import_failures",
                column: "roster_import_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_national_id",
                table: "students",
                column: "national_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "roster_import_entries");

            migrationBuilder.DropTable(
                name: "roster_import_failures");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "roster_imports");
        }
    }
}
