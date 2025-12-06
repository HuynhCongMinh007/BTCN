using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsTemplateField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTemplate",
                table: "DrawingTemplates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "DrawingTemplates",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "IsTemplate" },
                values: new object[] { new DateTime(2025, 12, 7, 1, 35, 34, 990, DateTimeKind.Local).AddTicks(5235), false });

            migrationBuilder.UpdateData(
                table: "DrawingTemplates",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "IsTemplate" },
                values: new object[] { new DateTime(2025, 12, 7, 1, 35, 34, 990, DateTimeKind.Local).AddTicks(5238), false });

            migrationBuilder.UpdateData(
                table: "Profiles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 7, 1, 35, 34, 990, DateTimeKind.Local).AddTicks(5144));

            migrationBuilder.UpdateData(
                table: "Shapes",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 7, 1, 35, 34, 990, DateTimeKind.Local).AddTicks(5251));

            migrationBuilder.UpdateData(
                table: "Shapes",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 7, 1, 35, 34, 990, DateTimeKind.Local).AddTicks(5253));

            migrationBuilder.UpdateData(
                table: "Shapes",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 7, 1, 35, 34, 990, DateTimeKind.Local).AddTicks(5256));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTemplate",
                table: "DrawingTemplates");

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
                column: "CreatedAt",
                value: new DateTime(2025, 12, 6, 14, 59, 30, 539, DateTimeKind.Local).AddTicks(7643));

            migrationBuilder.UpdateData(
                table: "Shapes",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 6, 14, 59, 30, 539, DateTimeKind.Local).AddTicks(7646));

            migrationBuilder.UpdateData(
                table: "Shapes",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 6, 14, 59, 30, 539, DateTimeKind.Local).AddTicks(7648));
        }
    }
}
