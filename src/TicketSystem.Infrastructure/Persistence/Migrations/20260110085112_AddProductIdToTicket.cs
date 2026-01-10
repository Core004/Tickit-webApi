using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductIdToTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ProductId",
                table: "Tickets",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Products_ProductId",
                table: "Tickets",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Products_ProductId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ProductId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Tickets");
        }
    }
}
