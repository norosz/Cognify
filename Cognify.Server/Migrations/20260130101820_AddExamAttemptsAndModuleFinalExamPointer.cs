using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognify.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddExamAttemptsAndModuleFinalExamPointer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentFinalExamQuizId",
                table: "Modules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExamAttemptId",
                table: "LearningInteractions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExamAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuizId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnswersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false),
                    TimeSpentSeconds = table.Column<int>(type: "int", nullable: true),
                    Difficulty = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAttempts_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamAttempts_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamAttempts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Modules_CurrentFinalExamQuizId",
                table: "Modules",
                column: "CurrentFinalExamQuizId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningInteractions_ExamAttemptId",
                table: "LearningInteractions",
                column: "ExamAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_ModuleId",
                table: "ExamAttempts",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_QuizId",
                table: "ExamAttempts",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_UserId",
                table: "ExamAttempts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_LearningInteractions_ExamAttempts_ExamAttemptId",
                table: "LearningInteractions",
                column: "ExamAttemptId",
                principalTable: "ExamAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Modules_Quizzes_CurrentFinalExamQuizId",
                table: "Modules",
                column: "CurrentFinalExamQuizId",
                principalTable: "Quizzes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LearningInteractions_ExamAttempts_ExamAttemptId",
                table: "LearningInteractions");

            migrationBuilder.DropForeignKey(
                name: "FK_Modules_Quizzes_CurrentFinalExamQuizId",
                table: "Modules");

            migrationBuilder.DropTable(
                name: "ExamAttempts");

            migrationBuilder.DropIndex(
                name: "IX_Modules_CurrentFinalExamQuizId",
                table: "Modules");

            migrationBuilder.DropIndex(
                name: "IX_LearningInteractions_ExamAttemptId",
                table: "LearningInteractions");

            migrationBuilder.DropColumn(
                name: "CurrentFinalExamQuizId",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "ExamAttemptId",
                table: "LearningInteractions");
        }
    }
}
