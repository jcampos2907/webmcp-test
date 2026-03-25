using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikePOS.Migrations
{
    /// <inheritdoc />
    public partial class AddErpIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "ServiceTicket",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSource",
                table: "ServiceTicket",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Product",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSource",
                table: "Product",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Customer",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSource",
                table: "Customer",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Component",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSource",
                table: "Component",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Charge",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSource",
                table: "Charge",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ErpConnection",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BaseUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ApiKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SyncCustomers = table.Column<bool>(type: "INTEGER", nullable: false),
                    SyncProducts = table.Column<bool>(type: "INTEGER", nullable: false),
                    SyncTickets = table.Column<bool>(type: "INTEGER", nullable: false),
                    SyncCharges = table.Column<bool>(type: "INTEGER", nullable: false),
                    SyncComponents = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StoreId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpConnection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErpConnection_Store_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Store",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncFieldMapping",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ErpConnectionId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LocalField = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RemoteField = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TransformExpression = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncFieldMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncFieldMapping_ErpConnection_ErpConnectionId",
                        column: x => x.ErpConnectionId,
                        principalTable: "ErpConnection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncLog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ErpConnectionId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Direction = table.Column<int>(type: "INTEGER", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestPayload = table.Column<string>(type: "TEXT", nullable: true),
                    ResponsePayload = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StoreId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncLog_ErpConnection_ErpConnectionId",
                        column: x => x.ErpConnectionId,
                        principalTable: "ErpConnection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SyncLog_Store_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Store",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErpConnection_StoreId_IsActive",
                table: "ErpConnection",
                columns: new[] { "StoreId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncFieldMapping_ErpConnectionId_EntityType_SortOrder",
                table: "SyncFieldMapping",
                columns: new[] { "ErpConnectionId", "EntityType", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncLog_EntityType_EntityId",
                table: "SyncLog",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncLog_ErpConnectionId",
                table: "SyncLog",
                column: "ErpConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLog_StoreId_CreatedAt",
                table: "SyncLog",
                columns: new[] { "StoreId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncFieldMapping");

            migrationBuilder.DropTable(
                name: "SyncLog");

            migrationBuilder.DropTable(
                name: "ErpConnection");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "ServiceTicket");

            migrationBuilder.DropColumn(
                name: "ExternalSource",
                table: "ServiceTicket");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "ExternalSource",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "ExternalSource",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Component");

            migrationBuilder.DropColumn(
                name: "ExternalSource",
                table: "Component");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Charge");

            migrationBuilder.DropColumn(
                name: "ExternalSource",
                table: "Charge");
        }
    }
}
