using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikePOS.Migrations
{
    /// <inheritdoc />
    public partial class RenameBikeToComponent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTicket_Bike_BikeId",
                table: "ServiceTicket");

            // Rename table Bike → Component
            migrationBuilder.RenameTable(
                name: "Bike",
                newName: "Component");

            // Add ComponentType column with default value
            migrationBuilder.AddColumn<string>(
                name: "ComponentType",
                table: "Component",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "Bicicleta");

            // Rename FK column BikeId → ComponentId on ServiceTicket
            migrationBuilder.RenameColumn(
                name: "BikeId",
                table: "ServiceTicket",
                newName: "ComponentId");

            migrationBuilder.RenameIndex(
                name: "IX_ServiceTicket_BikeId",
                table: "ServiceTicket",
                newName: "IX_ServiceTicket_ComponentId");

            // Rename PK and FK constraints
            migrationBuilder.RenameIndex(
                name: "IX_Bike_CustomerId",
                table: "Component",
                newName: "IX_Component_CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTicket_Component_ComponentId",
                table: "ServiceTicket",
                column: "ComponentId",
                principalTable: "Component",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTicket_Component_ComponentId",
                table: "ServiceTicket");

            migrationBuilder.DropColumn(
                name: "ComponentType",
                table: "Component");

            migrationBuilder.RenameTable(
                name: "Component",
                newName: "Bike");

            migrationBuilder.RenameColumn(
                name: "ComponentId",
                table: "ServiceTicket",
                newName: "BikeId");

            migrationBuilder.RenameIndex(
                name: "IX_ServiceTicket_ComponentId",
                table: "ServiceTicket",
                newName: "IX_ServiceTicket_BikeId");

            migrationBuilder.RenameIndex(
                name: "IX_Component_CustomerId",
                table: "Bike",
                newName: "IX_Bike_CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTicket_Bike_BikeId",
                table: "ServiceTicket",
                column: "BikeId",
                principalTable: "Bike",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
