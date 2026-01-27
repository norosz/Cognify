using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognify.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddExtractionStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "ExtractedContents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "ExtractedContents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ExtractedContents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "ExtractedContents");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ExtractedContents");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "ExtractedContents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
