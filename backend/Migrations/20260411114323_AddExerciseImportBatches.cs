using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToroFitDreaming4.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseImportBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExerciseImportBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CatalogVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CatalogPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TriggeredBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TotalExercises = table.Column<int>(type: "int", nullable: true),
                    Created = table.Column<int>(type: "int", nullable: true),
                    Updated = table.Column<int>(type: "int", nullable: true),
                    Unchanged = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseImportBatches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseImportBatches_ImportedAtUtc",
                table: "ExerciseImportBatches",
                column: "ImportedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseImportBatches");
        }
    }
}
