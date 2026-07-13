using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrivingLessons.Infrastructure.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddPublications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "publications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    week_start = table.Column<DateOnly>(type: "date", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    link_token = table.Column<string>(type: "text", nullable: false),
                    window_start_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    window_end_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_publications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "publication_teacher_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    publication_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_publication_teacher_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_publication_teacher_versions_publications_publication_id",
                        column: x => x.publication_id,
                        principalTable: "publications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_publication_teacher_versions_publication_id",
                table: "publication_teacher_versions",
                column: "publication_id");

            migrationBuilder.CreateIndex(
                name: "IX_publications_link_token",
                table: "publications",
                column: "link_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_publications_week_start",
                table: "publications",
                column: "week_start",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "publication_teacher_versions");

            migrationBuilder.DropTable(
                name: "publications");
        }
    }
}
