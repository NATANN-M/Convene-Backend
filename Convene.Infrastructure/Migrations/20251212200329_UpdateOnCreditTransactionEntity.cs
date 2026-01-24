using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convene.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOnCreditTransactionEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChapaCheckoutUrl",
                table: "CreditTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "CreditTransactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "CreditTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "CreditTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "CreditTransactions",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChapaCheckoutUrl",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "CreditTransactions");
        }
    }
}
