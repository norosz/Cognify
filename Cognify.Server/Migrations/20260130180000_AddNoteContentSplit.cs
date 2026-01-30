using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognify.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteContentSplit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiContent",
                table: "Notes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserContent",
                table: "Notes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("UPDATE Notes SET UserContent = Content WHERE UserContent IS NULL AND Content IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiContent",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "UserContent",
                table: "Notes");
        }
    }
}
