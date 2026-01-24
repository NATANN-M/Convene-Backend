using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convene.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceKeyToNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenceKey",
                table: "Notifications",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferenceKey",
                table: "Notifications");
        }
    }
}
