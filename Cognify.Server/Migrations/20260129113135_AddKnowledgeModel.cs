using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognify.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearningInteractions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningInteractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningInteractions_Attempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "Attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LearningInteractions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserKnowledgeStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MasteryScore = table.Column<double>(type: "float", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: false),
                    ForgettingRisk = table.Column<double>(type: "float", nullable: false),
                    NextReviewAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MistakePatternsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserKnowledgeStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserKnowledgeStates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnswerEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningInteractionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false),
                    MaxScore = table.Column<double>(type: "float", nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DetectedMistakesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfidenceEstimate = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerEvaluations_LearningInteractions_LearningInteractionId",
                        column: x => x.LearningInteractionId,
                        principalTable: "LearningInteractions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerEvaluations_LearningInteractionId",
                table: "AnswerEvaluations",
                column: "LearningInteractionId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningInteractions_AttemptId",
                table: "LearningInteractions",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningInteractions_UserId",
                table: "LearningInteractions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserKnowledgeStates_UserId_Topic",
                table: "UserKnowledgeStates",
                columns: new[] { "UserId", "Topic" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnswerEvaluations");

            migrationBuilder.DropTable(
                name: "UserKnowledgeStates");

            migrationBuilder.DropTable(
                name: "LearningInteractions");
        }
    }
}
