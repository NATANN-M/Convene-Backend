using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convene.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreaditAndPlatformRelatedEntityImplemntaions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "BookingId",
                table: "Payments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizerProfileId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BoostLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreditCost = table.Column<int>(type: "integer", nullable: false),
                    DurationHours = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoostLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditsChanged = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditTransactions_OrganizerProfiles_OrganizerProfileId",
                        column: x => x.OrganizerProfileId,
                        principalTable: "OrganizerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizerCreditBalance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<int>(type: "integer", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizerCreditBalance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizerCreditBalance_OrganizerProfiles_OrganizerProfileId",
                        column: x => x.OrganizerProfileId,
                        principalTable: "OrganizerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlatformSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InitialOrganizerCredits = table.Column<int>(type: "integer", nullable: false),
                    EventPublishCost = table.Column<int>(type: "integer", nullable: false),
                    CreditPriceETB = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventBoosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    BoostLevelId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditsUsed = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventBoosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventBoosts_BoostLevels_BoostLevelId",
                        column: x => x.BoostLevelId,
                        principalTable: "BoostLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventBoosts_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventBoosts_OrganizerProfiles_OrganizerProfileId",
                        column: x => x.OrganizerProfileId,
                        principalTable: "OrganizerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrganizerProfileId",
                table: "Payments",
                column: "OrganizerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_OrganizerProfileId",
                table: "CreditTransactions",
                column: "OrganizerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_EventBoosts_BoostLevelId",
                table: "EventBoosts",
                column: "BoostLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_EventBoosts_EventId",
                table: "EventBoosts",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventBoosts_OrganizerProfileId",
                table: "EventBoosts",
                column: "OrganizerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizerCreditBalance_OrganizerProfileId",
                table: "OrganizerCreditBalance",
                column: "OrganizerProfileId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_OrganizerProfiles_OrganizerProfileId",
                table: "Payments",
                column: "OrganizerProfileId",
                principalTable: "OrganizerProfiles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_OrganizerProfiles_OrganizerProfileId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "CreditTransactions");

            migrationBuilder.DropTable(
                name: "EventBoosts");

            migrationBuilder.DropTable(
                name: "OrganizerCreditBalance");

            migrationBuilder.DropTable(
                name: "PlatformSettings");

            migrationBuilder.DropTable(
                name: "BoostLevels");

            migrationBuilder.DropIndex(
                name: "IX_Payments_OrganizerProfileId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "OrganizerProfileId",
                table: "Payments");

            migrationBuilder.AlterColumn<Guid>(
                name: "BookingId",
                table: "Payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
