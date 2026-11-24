using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalPortal.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SplitAddressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Residences");

            migrationBuilder.DropColumn(
                name: "CurrentAddress",
                table: "RentalApplications");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Residences",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Residences",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Residences",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Residences",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrentCity",
                table: "RentalApplications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentPostalCode",
                table: "RentalApplications",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentState",
                table: "RentalApplications",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentStreet",
                table: "RentalApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Residences");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Residences");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Residences");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "Residences");

            migrationBuilder.DropColumn(
                name: "CurrentCity",
                table: "RentalApplications");

            migrationBuilder.DropColumn(
                name: "CurrentPostalCode",
                table: "RentalApplications");

            migrationBuilder.DropColumn(
                name: "CurrentState",
                table: "RentalApplications");

            migrationBuilder.DropColumn(
                name: "CurrentStreet",
                table: "RentalApplications");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Residences",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrentAddress",
                table: "RentalApplications",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);
        }
    }
}
