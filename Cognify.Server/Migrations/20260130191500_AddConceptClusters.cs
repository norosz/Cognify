using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognify.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddConceptClusters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConceptClusterId",
                table: "UserKnowledgeStates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConceptClusters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConceptClusters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConceptClusters_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConceptTopics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConceptClusterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConceptTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConceptTopics_ConceptClusters_ConceptClusterId",
                        column: x => x.ConceptClusterId,
                        principalTable: "ConceptClusters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserKnowledgeStates_ConceptClusterId",
                table: "UserKnowledgeStates",
                column: "ConceptClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptClusters_ModuleId",
                table: "ConceptClusters",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptTopics_ConceptClusterId",
                table: "ConceptTopics",
                column: "ConceptClusterId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserKnowledgeStates_ConceptClusters_ConceptClusterId",
                table: "UserKnowledgeStates",
                column: "ConceptClusterId",
                principalTable: "ConceptClusters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserKnowledgeStates_ConceptClusters_ConceptClusterId",
                table: "UserKnowledgeStates");

            migrationBuilder.DropTable(
                name: "ConceptTopics");

            migrationBuilder.DropTable(
                name: "ConceptClusters");

            migrationBuilder.DropIndex(
                name: "IX_UserKnowledgeStates_ConceptClusterId",
                table: "UserKnowledgeStates");

            migrationBuilder.DropColumn(
                name: "ConceptClusterId",
                table: "UserKnowledgeStates");
        }
    }
}
