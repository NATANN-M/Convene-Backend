using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convene.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOnRecommendationMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProximityDistributionJson",
                table: "RecommendationMetrics",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TopCategoriesJson",
                table: "RecommendationMetrics",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TopEventsJson",
                table: "RecommendationMetrics",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TotalEvents",
                table: "RecommendationMetrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalUsers",
                table: "RecommendationMetrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProximityDistributionJson",
                table: "RecommendationMetrics");

            migrationBuilder.DropColumn(
                name: "TopCategoriesJson",
                table: "RecommendationMetrics");

            migrationBuilder.DropColumn(
                name: "TopEventsJson",
                table: "RecommendationMetrics");

            migrationBuilder.DropColumn(
                name: "TotalEvents",
                table: "RecommendationMetrics");

            migrationBuilder.DropColumn(
                name: "TotalUsers",
                table: "RecommendationMetrics");
        }
    }
}
