using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikePOS.Migrations
{
    /// <inheritdoc />
    public partial class TicketWorkflowHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "ServiceTicket",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "ServiceTicket",
                type: "decimal(5, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTicket_CustomerId",
                table: "ServiceTicket",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTicket_Customer_CustomerId",
                table: "ServiceTicket",
                column: "CustomerId",
                principalTable: "Customer",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTicket_Customer_CustomerId",
                table: "ServiceTicket");

            migrationBuilder.DropIndex(
                name: "IX_ServiceTicket_CustomerId",
                table: "ServiceTicket");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "ServiceTicket");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "ServiceTicket");
        }
    }
}
