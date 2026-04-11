using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToroFitDreaming4.Migrations
{
    /// <inheritdoc />
    public partial class ExpandExerciseCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Exercises",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Exercises",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyRegion",
                table: "Exercises",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Exercises",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Exercises",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DifficultyLevel",
                table: "Exercises",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Exercises",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnilateral",
                table: "Exercises",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MovementPattern",
                table: "Exercises",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryEquipment",
                table: "Exercises",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryMuscleGroup",
                table: "Exercises",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SearchTerms",
                table: "Exercises",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SecondaryEquipment",
                table: "Exercises",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Exercises",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TrainingStyle",
                table: "Exercises",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Exercises",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql("""
                UPDATE Exercises
                SET
                    BodyRegion = 'Full Body',
                    Category = 'General',
                    CreatedAtUtc = SYSUTCDATETIME(),
                    DifficultyLevel = 'Beginner',
                    MovementPattern = 'General',
                    PrimaryMuscleGroup = 'General',
                    SearchTerms = LOWER(CONCAT(COALESCE(Name, ''), ' ', COALESCE(Description, ''), ' general full body general general beginner strength')),
                    Slug = CONCAT(
                        COALESCE(NULLIF(LOWER(REPLACE(REPLACE(LTRIM(RTRIM(Name)), '''', ''), ' ', '-')), ''), 'exercise'),
                        '-',
                        Id),
                    TrainingStyle = 'Strength',
                    UpdatedAtUtc = SYSUTCDATETIME()
                WHERE Slug = '';
                """);

            migrationBuilder.CreateTable(
                name: "ExerciseAliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExerciseId = table.Column<int>(type: "int", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseAliases_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseSecondaryMuscles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExerciseId = table.Column<int>(type: "int", nullable: false),
                    MuscleGroup = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseSecondaryMuscles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseSecondaryMuscles_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_BodyRegion",
                table: "Exercises",
                column: "BodyRegion");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_Category",
                table: "Exercises",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_IsArchived_Name",
                table: "Exercises",
                columns: new[] { "IsArchived", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_MovementPattern",
                table: "Exercises",
                column: "MovementPattern");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_PrimaryEquipment",
                table: "Exercises",
                column: "PrimaryEquipment");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_PrimaryMuscleGroup",
                table: "Exercises",
                column: "PrimaryMuscleGroup");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_Slug",
                table: "Exercises",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_TrainingStyle",
                table: "Exercises",
                column: "TrainingStyle");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseAliases_ExerciseId_Alias",
                table: "ExerciseAliases",
                columns: new[] { "ExerciseId", "Alias" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSecondaryMuscles_ExerciseId_MuscleGroup",
                table: "ExerciseSecondaryMuscles",
                columns: new[] { "ExerciseId", "MuscleGroup" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseAliases");

            migrationBuilder.DropTable(
                name: "ExerciseSecondaryMuscles");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_BodyRegion",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_Category",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_IsArchived_Name",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_MovementPattern",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_PrimaryEquipment",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_PrimaryMuscleGroup",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_Slug",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_TrainingStyle",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "BodyRegion",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "DifficultyLevel",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "IsUnilateral",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "MovementPattern",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "PrimaryEquipment",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "PrimaryMuscleGroup",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "SearchTerms",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "SecondaryEquipment",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "TrainingStyle",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Exercises");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Exercises",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Exercises",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
