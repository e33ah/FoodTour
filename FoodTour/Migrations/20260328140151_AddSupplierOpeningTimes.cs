using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTour.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierOpeningTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpeningTimes",
                table: "Suppliers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpeningTimes",
                table: "Suppliers");
        }
    }
}
