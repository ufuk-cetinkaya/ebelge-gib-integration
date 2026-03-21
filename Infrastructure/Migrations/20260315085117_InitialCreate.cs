using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Envelope",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstanceIdentifier = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    PackageType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SenderIdentifier = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    SenderTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SenderAlias = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ReceiverIdentifier = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    ReceiverTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ReceiverAlias = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ResponseCode = table.Column<int>(type: "int", nullable: true),
                    ResponseDesc = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Direction = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SubStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StatusCheck = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Envelope", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Report",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Hazirlayan = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Mukellef = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    RaporNo = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    DonemBaslangic = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DonemBitis = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BolumBaslangic = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BolumBitis = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BolumNo = table.Column<int>(type: "int", nullable: false),
                    BelgeSayisi = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SubStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorDesc = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ResponseCode = table.Column<int>(type: "int", nullable: true),
                    ResponseDesc = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Report", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Document",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnvelopeId = table.Column<int>(type: "int", nullable: true),
                    ReportId = table.Column<int>(type: "int", nullable: true),
                    ProfileId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DocumentId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Uuid = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TypeCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PayableAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    SupplierIdentifier = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    SupplierTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CustomerIdentifier = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    CustomerTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RefId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    ResponseCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ResponseDesc = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Direction = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SubStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorDesc = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CancelFlag = table.Column<bool>(type: "bit", nullable: false),
                    CancelDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelReportId = table.Column<int>(type: "int", nullable: true),
                    SigningTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Document", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Document_Envelope_EnvelopeId",
                        column: x => x.EnvelopeId,
                        principalTable: "Envelope",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Document_Report_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Report",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Document_EnvelopeId",
                table: "Document",
                column: "EnvelopeId");

            migrationBuilder.CreateIndex(
                name: "IX_Document_ReportId",
                table: "Document",
                column: "ReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Document");

            migrationBuilder.DropTable(
                name: "Envelope");

            migrationBuilder.DropTable(
                name: "Report");
        }
    }
}
