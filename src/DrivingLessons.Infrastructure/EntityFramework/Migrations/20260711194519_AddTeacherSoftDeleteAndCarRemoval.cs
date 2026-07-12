using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrivingLessons.Infrastructure.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherSoftDeleteAndCarRemoval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "teachers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_removed",
                table: "cars",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "teachers");

            migrationBuilder.DropColumn(
                name: "is_removed",
                table: "cars");
        }
    }
}
