using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyAddressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PostalCode",
                table: "Companies",
                newName: "PinCode");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Companies",
                newName: "PhoneNo");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Area",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobileNo",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressLine1",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "MobileNo",
                table: "Companies");

            migrationBuilder.RenameColumn(
                name: "PinCode",
                table: "Companies",
                newName: "PostalCode");

            migrationBuilder.RenameColumn(
                name: "PhoneNo",
                table: "Companies",
                newName: "Phone");
        }
    }
}
