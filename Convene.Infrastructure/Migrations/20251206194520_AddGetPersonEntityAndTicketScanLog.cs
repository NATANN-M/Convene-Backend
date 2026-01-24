using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convene.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGetPersonEntityAndTicketScanLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketScanLogs_Tickets_TicketId",
                table: "TicketScanLogs");

            migrationBuilder.DropColumn(
                name: "ScannedBy",
                table: "TicketScanLogs");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "TicketScanLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "TicketScanLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EventEnd",
                table: "TicketScanLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "TicketScanLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "EventName",
                table: "TicketScanLogs",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EventStart",
                table: "TicketScanLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsValid",
                table: "TicketScanLogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "TicketScanLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScannerEmail",
                table: "TicketScanLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScannerName",
                table: "TicketScanLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScannerUserId",
                table: "TicketScanLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TicketHolderEmail",
                table: "TicketScanLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TicketHolderName",
                table: "TicketScanLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TicketTypeName",
                table: "TicketScanLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "GatePersons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByOrganizerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentsJson = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GatePersons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GatePersons_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketScanLogs_EventId",
                table: "TicketScanLogs",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketScanLogs_IsValid",
                table: "TicketScanLogs",
                column: "IsValid");

            migrationBuilder.CreateIndex(
                name: "IX_TicketScanLogs_ScannedAt",
                table: "TicketScanLogs",
                column: "ScannedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TicketScanLogs_ScannerUserId",
                table: "TicketScanLogs",
                column: "ScannerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GatePersons_CreatedByOrganizerId",
                table: "GatePersons",
                column: "CreatedByOrganizerId");

            migrationBuilder.CreateIndex(
                name: "IX_GatePersons_IsActive",
                table: "GatePersons",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_GatePersons_UserId",
                table: "GatePersons",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GatePersons");

            migrationBuilder.DropIndex(
                name: "IX_TicketScanLogs_EventId",
                table: "TicketScanLogs");

            migrationBuilder.DropIndex(
                name: "IX_TicketScanLogs_IsValid",
                table: "TicketScanLogs");

            migrationBuilder.DropIndex(
                name: "IX_TicketScanLogs_ScannedAt",
                table: "TicketScanLogs");

            migrationBuilder.DropIndex(
                name: "IX_TicketScanLogs_ScannerUserId",
                table: "TicketScanLogs");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "TicketScanLogs");

            migrationBuilder.DropColumn(
                name: "EventEnd",
                table: "TicketScanLogs");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "TicketScanLogs");

            migrationBuilder.DropColumn(
                name: "EventName",
                table: "TicketScanLogs");

            migrationBuilder.DropColumn(
                name: "EventStart",
                table: "TicketScanLogs");

            migrationBuilder.DropColumn(
                name: "IsValid",
                table: "TicketScanLogs");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "TicketScanLogs");

            migrationBuilder.DropColumn(
                name: "ScannerEmail",
                table: "TicketScanLogs");

            migrationBuilder.DropColumn(
                name: "ScannerName",
                table: "TicketScanLogs");

            migrationBuilder.DropColumn(
                name: "ScannerUserId",
                table: "TicketScanLogs");

            migrationBuilder.DropColumn(
                name: "TicketHolderEmail",
                table: "TicketScanLogs");

            migrationBuilder.DropColumn(
                name: "TicketHolderName",
                table: "TicketScanLogs");

            migrationBuilder.DropColumn(
                name: "TicketTypeName",
                table: "TicketScanLogs");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "TicketScanLogs",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScannedBy",
                table: "TicketScanLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketScanLogs_Tickets_TicketId",
                table: "TicketScanLogs",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
