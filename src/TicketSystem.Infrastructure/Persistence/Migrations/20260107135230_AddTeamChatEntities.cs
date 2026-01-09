using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamChatEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupChats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsPrivate = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupChats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupChats_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupChatMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupChatId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsMuted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupChatMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupChatMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupChatMembers_GroupChats_GroupChatId",
                        column: x => x.GroupChatId,
                        principalTable: "GroupChats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamId = table.Column<int>(type: "int", nullable: true),
                    GroupChatId = table.Column<int>(type: "int", nullable: true),
                    SenderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageType = table.Column<int>(type: "int", nullable: false),
                    IsEdited = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplyToMessageId = table.Column<int>(type: "int", nullable: true),
                    TeamMessageId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMessages_AspNetUsers_SenderId",
                        column: x => x.SenderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamMessages_GroupChats_GroupChatId",
                        column: x => x.GroupChatId,
                        principalTable: "GroupChats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMessages_TeamMessages_ReplyToMessageId",
                        column: x => x.ReplyToMessageId,
                        principalTable: "TeamMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamMessages_TeamMessages_TeamMessageId",
                        column: x => x.TeamMessageId,
                        principalTable: "TeamMessages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeamMessages_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamMessageAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMessageAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMessageAttachments_TeamMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "TeamMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamMessageEditHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<int>(type: "int", nullable: false),
                    PreviousContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EditedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMessageEditHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMessageEditHistories_AspNetUsers_EditedById",
                        column: x => x.EditedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMessageEditHistories_TeamMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "TeamMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamMessageReactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Emoji = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMessageReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMessageReactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamMessageReactions_TeamMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "TeamMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupChatMembers_GroupChatId",
                table: "GroupChatMembers",
                column: "GroupChatId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupChatMembers_UserId",
                table: "GroupChatMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupChats_CreatedById",
                table: "GroupChats",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMessageAttachments_MessageId",
                table: "TeamMessageAttachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMessageEditHistories_EditedById",
                table: "TeamMessageEditHistories",
                column: "EditedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMessageEditHistories_MessageId",
                table: "TeamMessageEditHistories",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMessageReactions_MessageId",
                table: "TeamMessageReactions",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMessageReactions_UserId",
                table: "TeamMessageReactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMessages_GroupChatId",
                table: "TeamMessages",
                column: "GroupChatId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMessages_ReplyToMessageId",
                table: "TeamMessages",
                column: "ReplyToMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMessages_SenderId",
                table: "TeamMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMessages_TeamId",
                table: "TeamMessages",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMessages_TeamMessageId",
                table: "TeamMessages",
                column: "TeamMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupChatMembers");

            migrationBuilder.DropTable(
                name: "TeamMessageAttachments");

            migrationBuilder.DropTable(
                name: "TeamMessageEditHistories");

            migrationBuilder.DropTable(
                name: "TeamMessageReactions");

            migrationBuilder.DropTable(
                name: "TeamMessages");

            migrationBuilder.DropTable(
                name: "GroupChats");
        }
    }
}
