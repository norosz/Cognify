using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognify.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryFieldsToModuleAndQuiz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoryLabel",
                table: "Quizzes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategorySource",
                table: "Quizzes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryLabel",
                table: "Modules",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategorySource",
                table: "Modules",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryLabel",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "CategorySource",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "CategoryLabel",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "CategorySource",
                table: "Modules");
        }
    }
}
