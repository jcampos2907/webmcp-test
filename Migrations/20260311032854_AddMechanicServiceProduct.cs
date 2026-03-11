using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikePOS.Migrations
{
    /// <inheritdoc />
    public partial class AddMechanicServiceProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTicket_Bike_BikeId",
                table: "ServiceTicket");

            migrationBuilder.CreateTable(
                name: "Mechanic",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mechanic", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    QuantityInStock = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Service",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultPrice = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Service", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TicketProduct",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceTicketId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketProduct", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketProduct_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketProduct_ServiceTicket_ServiceTicketId",
                        column: x => x.ServiceTicketId,
                        principalTable: "ServiceTicket",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTicket_BaseServiceId",
                table: "ServiceTicket",
                column: "BaseServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTicket_MechanicId",
                table: "ServiceTicket",
                column: "MechanicId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketProduct_ProductId",
                table: "TicketProduct",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketProduct_ServiceTicketId_ProductId",
                table: "TicketProduct",
                columns: new[] { "ServiceTicketId", "ProductId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTicket_Bike_BikeId",
                table: "ServiceTicket",
                column: "BikeId",
                principalTable: "Bike",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTicket_Mechanic_MechanicId",
                table: "ServiceTicket",
                column: "MechanicId",
                principalTable: "Mechanic",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTicket_Service_BaseServiceId",
                table: "ServiceTicket",
                column: "BaseServiceId",
                principalTable: "Service",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTicket_Bike_BikeId",
                table: "ServiceTicket");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTicket_Mechanic_MechanicId",
                table: "ServiceTicket");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTicket_Service_BaseServiceId",
                table: "ServiceTicket");

            migrationBuilder.DropTable(
                name: "Mechanic");

            migrationBuilder.DropTable(
                name: "Service");

            migrationBuilder.DropTable(
                name: "TicketProduct");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropIndex(
                name: "IX_ServiceTicket_BaseServiceId",
                table: "ServiceTicket");

            migrationBuilder.DropIndex(
                name: "IX_ServiceTicket_MechanicId",
                table: "ServiceTicket");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTicket_Bike_BikeId",
                table: "ServiceTicket",
                column: "BikeId",
                principalTable: "Bike",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
