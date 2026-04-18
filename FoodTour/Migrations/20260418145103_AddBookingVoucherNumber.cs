using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTour.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingVoucherNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoucherNumber",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoucherNumber",
                table: "Bookings");
        }
    }
}
