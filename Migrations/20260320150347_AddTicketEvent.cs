using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikePOS.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketEvent",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ServiceTicketId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    EventType = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Details = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    StoreId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketEvent_ServiceTicket_ServiceTicketId",
                        column: x => x.ServiceTicketId,
                        principalTable: "ServiceTicket",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketEvent_Store_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Store",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketEvent_CreatedAt",
                table: "TicketEvent",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TicketEvent_ServiceTicketId",
                table: "TicketEvent",
                column: "ServiceTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketEvent_StoreId",
                table: "TicketEvent",
                column: "StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketEvent");
        }
    }
}
