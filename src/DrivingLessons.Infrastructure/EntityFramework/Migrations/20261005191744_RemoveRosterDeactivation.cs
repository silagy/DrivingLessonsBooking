using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrivingLessons.Infrastructure.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRosterDeactivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM roster_import_entries WHERE outcome = 30;");

            migrationBuilder.DropColumn(
                name: "deactivated_count",
                table: "roster_imports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "deactivated_count",
                table: "roster_imports",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
