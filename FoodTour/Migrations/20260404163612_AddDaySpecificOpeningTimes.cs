using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTour.Migrations
{
    /// <inheritdoc />
    public partial class AddDaySpecificOpeningTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpeningTimesMonday",
                table: "Suppliers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpeningTimesTuesday",
                table: "Suppliers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpeningTimesWednesday",
                table: "Suppliers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpeningTimesThursday",
                table: "Suppliers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpeningTimesFriday",
                table: "Suppliers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpeningTimesSaturday",
                table: "Suppliers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpeningTimesSunday",
                table: "Suppliers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpeningTimesSunday",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "OpeningTimesSaturday",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "OpeningTimesFriday",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "OpeningTimesThursday",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "OpeningTimesWednesday",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "OpeningTimesTuesday",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "OpeningTimesMonday",
                table: "Suppliers");
        }
    }
}
