using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DateColumnsRenamed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ModifyDate",
                table: "Report",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreateDate",
                table: "Report",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ModifyDate",
                table: "Envelope",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreateDate",
                table: "Envelope",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreateDate",
                table: "Document",
                newName: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Report",
                newName: "ModifyDate");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Report",
                newName: "CreateDate");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Envelope",
                newName: "ModifyDate");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Envelope",
                newName: "CreateDate");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Document",
                newName: "CreateDate");
        }
    }
}
