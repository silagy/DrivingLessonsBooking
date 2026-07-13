using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrivingLessons.Infrastructure.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class PromoteCarToSharedPool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "car_teachers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    car_id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_car_teachers", x => x.id);
                    table.ForeignKey(
                        name: "FK_car_teachers_cars_car_id",
                        column: x => x.car_id,
                        principalTable: "cars",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                "INSERT INTO car_teachers (id, car_id, teacher_id) SELECT gen_random_uuid(), id, teacher_id FROM cars;");

            migrationBuilder.DropForeignKey(name: "FK_cars_teachers_teacher_id", table: "cars");

            migrationBuilder.DropIndex(name: "IX_cars_teacher_id", table: "cars");

            migrationBuilder.DropColumn(name: "teacher_id", table: "cars");

            migrationBuilder.RenameColumn(name: "is_removed", table: "cars", newName: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_car_teachers_car_id_teacher_id",
                table: "car_teachers",
                columns: new[] { "car_id", "teacher_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(name: "is_deleted", table: "cars", newName: "is_removed");

            migrationBuilder.AddColumn<Guid>(name: "teacher_id", table: "cars", type: "uuid", nullable: true);

            migrationBuilder.Sql(
                "UPDATE cars SET teacher_id = (SELECT teacher_id FROM car_teachers WHERE car_teachers.car_id = cars.id LIMIT 1);");

            migrationBuilder.Sql("DELETE FROM cars WHERE teacher_id IS NULL;");

            migrationBuilder.AlterColumn<Guid>(name: "teacher_id", table: "cars", type: "uuid", nullable: false);

            migrationBuilder.AddForeignKey(
                name: "FK_cars_teachers_teacher_id",
                table: "cars",
                column: "teacher_id",
                principalTable: "teachers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.CreateIndex(name: "IX_cars_teacher_id", table: "cars", column: "teacher_id");

            migrationBuilder.DropTable(name: "car_teachers");
        }
    }
}
