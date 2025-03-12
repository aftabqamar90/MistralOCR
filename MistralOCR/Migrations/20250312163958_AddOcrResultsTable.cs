using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MistralOCR.Migrations
{
    /// <inheritdoc />
    public partial class AddOcrResultsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OcrResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    OcrResultJson = table.Column<string>(type: "TEXT", nullable: false),
                    PagesProcessed = table.Column<int>(type: "INTEGER", nullable: false),
                    DocSizeBytes = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcrResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OcrResults_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OcrResults_CreatedAt",
                table: "OcrResults",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OcrResults_DocumentId",
                table: "OcrResults",
                column: "DocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OcrResults");
        }
    }
}
