using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrivingLessons.Infrastructure.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class MoveAdminToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO users (id, name, email, password_hash, role, teacher_id, security_stamp, is_deleted)
                SELECT a."Id", 'Administrator', a."Email", a."PasswordHash", 10, NULL,
                       replace(gen_random_uuid()::text, '-', ''), false
                FROM admin_users a
                WHERE NOT EXISTS (SELECT 1 FROM users u WHERE u.email = a."Email");
                """);

            migrationBuilder.DropTable(
                name: "admin_users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_users_Email",
                table: "admin_users",
                column: "Email",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO admin_users ("Id", "Email", "PasswordHash")
                SELECT id, email, password_hash
                FROM users
                WHERE role = 10 AND NOT is_deleted;
                """);
        }
    }
}
