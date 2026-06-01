using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTour.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitReimbursement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSupplierReimbursed",
                table: "Visits",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReimbursedAt",
                table: "Visits",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSupplierReimbursed",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "ReimbursedAt",
                table: "Visits");
        }
    }
}
