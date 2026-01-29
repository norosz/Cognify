using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognify.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentRunTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AgentRunId",
                table: "PendingQuizzes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AgentRunId",
                table: "ExtractedContents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PromptVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OutputJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PromptTokens = table.Column<int>(type: "int", nullable: true),
                    CompletionTokens = table.Column<int>(type: "int", nullable: true),
                    TotalTokens = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentRuns_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingQuizzes_AgentRunId",
                table: "PendingQuizzes",
                column: "AgentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractedContents_AgentRunId",
                table: "ExtractedContents",
                column: "AgentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_UserId",
                table: "AgentRuns",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExtractedContents_AgentRuns_AgentRunId",
                table: "ExtractedContents",
                column: "AgentRunId",
                principalTable: "AgentRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PendingQuizzes_AgentRuns_AgentRunId",
                table: "PendingQuizzes",
                column: "AgentRunId",
                principalTable: "AgentRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExtractedContents_AgentRuns_AgentRunId",
                table: "ExtractedContents");

            migrationBuilder.DropForeignKey(
                name: "FK_PendingQuizzes_AgentRuns_AgentRunId",
                table: "PendingQuizzes");

            migrationBuilder.DropTable(
                name: "AgentRuns");

            migrationBuilder.DropIndex(
                name: "IX_PendingQuizzes_AgentRunId",
                table: "PendingQuizzes");

            migrationBuilder.DropIndex(
                name: "IX_ExtractedContents_AgentRunId",
                table: "ExtractedContents");

            migrationBuilder.DropColumn(
                name: "AgentRunId",
                table: "PendingQuizzes");

            migrationBuilder.DropColumn(
                name: "AgentRunId",
                table: "ExtractedContents");
        }
    }
}
