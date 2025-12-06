using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStrokeStyleToShape : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StrokeStyle",
                table: "Shapes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "DrawingTemplates",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 6, 14, 59, 30, 539, DateTimeKind.Local).AddTicks(7625));

            migrationBuilder.UpdateData(
                table: "DrawingTemplates",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 6, 14, 59, 30, 539, DateTimeKind.Local).AddTicks(7628));

            migrationBuilder.UpdateData(
                table: "Profiles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 6, 14, 59, 30, 539, DateTimeKind.Local).AddTicks(7525));

            migrationBuilder.UpdateData(
                table: "Shapes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "StrokeStyle" },
                values: new object[] { new DateTime(2025, 12, 6, 14, 59, 30, 539, DateTimeKind.Local).AddTicks(7643), "Solid" });

            migrationBuilder.UpdateData(
                table: "Shapes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "StrokeStyle" },
                values: new object[] { new DateTime(2025, 12, 6, 14, 59, 30, 539, DateTimeKind.Local).AddTicks(7646), "Solid" });

            migrationBuilder.UpdateData(
                table: "Shapes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "StrokeStyle" },
                values: new object[] { new DateTime(2025, 12, 6, 14, 59, 30, 539, DateTimeKind.Local).AddTicks(7648), "Solid" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StrokeStyle",
                table: "Shapes");

            migrationBuilder.UpdateData(
                table: "DrawingTemplates",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 5, 19, 17, 59, 843, DateTimeKind.Local).AddTicks(698));

            migrationBuilder.UpdateData(
                table: "DrawingTemplates",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 5, 19, 17, 59, 843, DateTimeKind.Local).AddTicks(700));

            migrationBuilder.UpdateData(
                table: "Profiles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 5, 19, 17, 59, 843, DateTimeKind.Local).AddTicks(592));

            migrationBuilder.UpdateData(
                table: "Shapes",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 5, 19, 17, 59, 843, DateTimeKind.Local).AddTicks(712));

            migrationBuilder.UpdateData(
                table: "Shapes",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 5, 19, 17, 59, 843, DateTimeKind.Local).AddTicks(714));

            migrationBuilder.UpdateData(
                table: "Shapes",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 5, 19, 17, 59, 843, DateTimeKind.Local).AddTicks(716));
        }
    }
}
