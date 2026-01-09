using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketDrafts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    PriorityId = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketDrafts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketDrafts_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketDrafts_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketDrafts_Priorities_PriorityId",
                        column: x => x.PriorityId,
                        principalTable: "Priorities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketDrafts_TicketCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "TicketCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketDrafts_CategoryId",
                table: "TicketDrafts",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketDrafts_CompanyId",
                table: "TicketDrafts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketDrafts_DepartmentId",
                table: "TicketDrafts",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketDrafts_PriorityId",
                table: "TicketDrafts",
                column: "PriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketDrafts_UserId",
                table: "TicketDrafts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketDrafts");
        }
    }
}
