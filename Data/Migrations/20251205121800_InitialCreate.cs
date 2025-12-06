using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DrawingTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Width = table.Column<double>(type: "REAL", nullable: false),
                    Height = table.Column<double>(type: "REAL", nullable: false),
                    BackgroundColor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrawingTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Theme = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DefaultCanvasWidth = table.Column<double>(type: "REAL", nullable: false),
                    DefaultCanvasHeight = table.Column<double>(type: "REAL", nullable: false),
                    DefaultStrokeColor = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultFillColor = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultBackgroundColor = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultStrokeThickness = table.Column<double>(type: "REAL", nullable: false),
                    CustomSettings = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Shapes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShapeType = table.Column<int>(type: "INTEGER", nullable: false),
                    PointsData = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StrokeThickness = table.Column<double>(type: "REAL", nullable: false),
                    IsFilled = table.Column<bool>(type: "INTEGER", nullable: false),
                    FillColor = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TemplateId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shapes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shapes_DrawingTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "DrawingTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DrawingTemplates",
                columns: new[] { "Id", "BackgroundColor", "CreatedAt", "Height", "ModifiedAt", "Name", "Width" },
                values: new object[,]
                {
                    { 1, "#F0F0F0", new DateTime(2025, 12, 5, 19, 17, 59, 843, DateTimeKind.Local).AddTicks(698), 600.0, null, "Sample Template 1", 800.0 },
                    { 2, "#E8F4F8", new DateTime(2025, 12, 5, 19, 17, 59, 843, DateTimeKind.Local).AddTicks(700), 768.0, null, "Sample Template 2", 1024.0 }
                });

            migrationBuilder.InsertData(
                table: "Profiles",
                columns: new[] { "Id", "CreatedAt", "CustomSettings", "DefaultBackgroundColor", "DefaultCanvasHeight", "DefaultCanvasWidth", "DefaultFillColor", "DefaultStrokeColor", "DefaultStrokeThickness", "IsActive", "ModifiedAt", "Name", "Theme" },
                values: new object[] { 1, new DateTime(2025, 12, 5, 19, 17, 59, 843, DateTimeKind.Local).AddTicks(592), null, "#FFFFFF", 600.0, 800.0, "#FFFFFF", "#000000", 2.0, true, null, "Default Profile", "System" });

            migrationBuilder.InsertData(
                table: "Shapes",
                columns: new[] { "Id", "Color", "CreatedAt", "FillColor", "IsFilled", "PointsData", "ShapeType", "StrokeThickness", "TemplateId" },
                values: new object[,]
                {
                    { 1, "#FF0000", new DateTime(2025, 12, 5, 19, 17, 59, 843, DateTimeKind.Local).AddTicks(712), "#FFCCCC", true, "[{\"X\":100,\"Y\":100},{\"X\":300,\"Y\":250}]", 1, 3.0, 1 },
                    { 2, "#0000FF", new DateTime(2025, 12, 5, 19, 17, 59, 843, DateTimeKind.Local).AddTicks(714), null, false, "[{\"X\":500,\"Y\":300},{\"X\":600,\"Y\":300}]", 3, 2.0, 1 },
                    { 3, "#00FF00", new DateTime(2025, 12, 5, 19, 17, 59, 843, DateTimeKind.Local).AddTicks(716), null, false, "[{\"X\":50,\"Y\":50},{\"X\":400,\"Y\":400}]", 0, 5.0, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shapes_TemplateId",
                table: "Shapes",
                column: "TemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "Shapes");

            migrationBuilder.DropTable(
                name: "DrawingTemplates");
        }
    }
}
