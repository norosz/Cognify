using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognify.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCategorySuggestionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategorySuggestionBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuizId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategorySuggestionBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategorySuggestionBatches_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CategorySuggestionBatches_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CategorySuggestionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: true),
                    Rationale = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategorySuggestionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategorySuggestionItems_CategorySuggestionBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "CategorySuggestionBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategorySuggestionBatches_ModuleId",
                table: "CategorySuggestionBatches",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_CategorySuggestionBatches_QuizId",
                table: "CategorySuggestionBatches",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_CategorySuggestionItems_BatchId",
                table: "CategorySuggestionItems",
                column: "BatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategorySuggestionItems");

            migrationBuilder.DropTable(
                name: "CategorySuggestionBatches");
        }
    }
}
