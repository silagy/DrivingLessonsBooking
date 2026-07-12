using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrivingLessons.Infrastructure.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddWeekSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "week_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    week_start = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_week_schedules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "slots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    day = table.Column<int>(type: "integer", nullable: false),
                    window = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    week_schedule_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_slots", x => x.id);
                    table.ForeignKey(
                        name: "FK_slots_week_schedules_week_schedule_id",
                        column: x => x.week_schedule_id,
                        principalTable: "week_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_slots_week_schedule_id",
                table: "slots",
                column: "week_schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_week_schedules_teacher_id_week_start",
                table: "week_schedules",
                columns: new[] { "teacher_id", "week_start" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "slots");

            migrationBuilder.DropTable(
                name: "week_schedules");
        }
    }
}
