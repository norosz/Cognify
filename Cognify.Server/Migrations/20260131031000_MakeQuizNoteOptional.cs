using System;
using Cognify.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognify.Server.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260131031000_MakeQuizNoteOptional")]
    public partial class MakeQuizNoteOptional : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PendingQuizzes_Notes_NoteId",
                table: "PendingQuizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Notes_NoteId",
                table: "Quizzes");

            migrationBuilder.AlterColumn<Guid>(
                name: "NoteId",
                table: "PendingQuizzes",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "NoteId",
                table: "Quizzes",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_PendingQuizzes_Notes_NoteId",
                table: "PendingQuizzes",
                column: "NoteId",
                principalTable: "Notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Notes_NoteId",
                table: "Quizzes",
                column: "NoteId",
                principalTable: "Notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PendingQuizzes_Notes_NoteId",
                table: "PendingQuizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Notes_NoteId",
                table: "Quizzes");

            migrationBuilder.AlterColumn<Guid>(
                name: "NoteId",
                table: "PendingQuizzes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "NoteId",
                table: "Quizzes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PendingQuizzes_Notes_NoteId",
                table: "PendingQuizzes",
                column: "NoteId",
                principalTable: "Notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Notes_NoteId",
                table: "Quizzes",
                column: "NoteId",
                principalTable: "Notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
