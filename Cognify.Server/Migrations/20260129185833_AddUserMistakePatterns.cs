using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognify.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMistakePatterns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserMistakePatterns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMistakePatterns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMistakePatterns_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMistakePatterns_UserId_Topic_Category",
                table: "UserMistakePatterns",
                columns: new[] { "UserId", "Topic", "Category" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserMistakePatterns");
        }
    }
}
