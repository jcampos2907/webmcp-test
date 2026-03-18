using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikePOS.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShopSetting_Key",
                table: "ShopSetting");

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "ShopSetting",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ServiceTicket",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "ServiceTicket",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ServiceTicket",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Service",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Product",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "MetaFieldDefinition",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Mechanic",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Customer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Component",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Charge",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Charge",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExternalSubjectId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Conglomerate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conglomerate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConglomerateId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Locale = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    TaxId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Company", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Company_Conglomerate_ConglomerateId",
                        column: x => x.ConglomerateId,
                        principalTable: "Conglomerate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Store",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Store", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Store_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AppUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    StoreId = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreUser_AppUser_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoreUser_Store_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Store",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopSetting_StoreId_Key",
                table: "ShopSetting",
                columns: new[] { "StoreId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTicket_StoreId",
                table: "ServiceTicket",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Service_StoreId",
                table: "Service",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_StoreId",
                table: "Product",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaFieldDefinition_StoreId",
                table: "MetaFieldDefinition",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Mechanic_StoreId",
                table: "Mechanic",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_StoreId",
                table: "Customer",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Component_StoreId",
                table: "Component",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Charge_StoreId",
                table: "Charge",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUser_ExternalSubjectId",
                table: "AppUser",
                column: "ExternalSubjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Company_ConglomerateId",
                table: "Company",
                column: "ConglomerateId");

            migrationBuilder.CreateIndex(
                name: "IX_Store_CompanyId",
                table: "Store",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreUser_AppUserId_StoreId",
                table: "StoreUser",
                columns: new[] { "AppUserId", "StoreId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreUser_StoreId",
                table: "StoreUser",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Charge_Store_StoreId",
                table: "Charge",
                column: "StoreId",
                principalTable: "Store",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Component_Store_StoreId",
                table: "Component",
                column: "StoreId",
                principalTable: "Store",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Customer_Store_StoreId",
                table: "Customer",
                column: "StoreId",
                principalTable: "Store",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Mechanic_Store_StoreId",
                table: "Mechanic",
                column: "StoreId",
                principalTable: "Store",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MetaFieldDefinition_Store_StoreId",
                table: "MetaFieldDefinition",
                column: "StoreId",
                principalTable: "Store",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Product_Store_StoreId",
                table: "Product",
                column: "StoreId",
                principalTable: "Store",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Service_Store_StoreId",
                table: "Service",
                column: "StoreId",
                principalTable: "Store",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTicket_Store_StoreId",
                table: "ServiceTicket",
                column: "StoreId",
                principalTable: "Store",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShopSetting_Store_StoreId",
                table: "ShopSetting",
                column: "StoreId",
                principalTable: "Store",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Charge_Store_StoreId",
                table: "Charge");

            migrationBuilder.DropForeignKey(
                name: "FK_Component_Store_StoreId",
                table: "Component");

            migrationBuilder.DropForeignKey(
                name: "FK_Customer_Store_StoreId",
                table: "Customer");

            migrationBuilder.DropForeignKey(
                name: "FK_Mechanic_Store_StoreId",
                table: "Mechanic");

            migrationBuilder.DropForeignKey(
                name: "FK_MetaFieldDefinition_Store_StoreId",
                table: "MetaFieldDefinition");

            migrationBuilder.DropForeignKey(
                name: "FK_Product_Store_StoreId",
                table: "Product");

            migrationBuilder.DropForeignKey(
                name: "FK_Service_Store_StoreId",
                table: "Service");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTicket_Store_StoreId",
                table: "ServiceTicket");

            migrationBuilder.DropForeignKey(
                name: "FK_ShopSetting_Store_StoreId",
                table: "ShopSetting");

            migrationBuilder.DropTable(
                name: "StoreUser");

            migrationBuilder.DropTable(
                name: "AppUser");

            migrationBuilder.DropTable(
                name: "Store");

            migrationBuilder.DropTable(
                name: "Company");

            migrationBuilder.DropTable(
                name: "Conglomerate");

            migrationBuilder.DropIndex(
                name: "IX_ShopSetting_StoreId_Key",
                table: "ShopSetting");

            migrationBuilder.DropIndex(
                name: "IX_ServiceTicket_StoreId",
                table: "ServiceTicket");

            migrationBuilder.DropIndex(
                name: "IX_Service_StoreId",
                table: "Service");

            migrationBuilder.DropIndex(
                name: "IX_Product_StoreId",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_MetaFieldDefinition_StoreId",
                table: "MetaFieldDefinition");

            migrationBuilder.DropIndex(
                name: "IX_Mechanic_StoreId",
                table: "Mechanic");

            migrationBuilder.DropIndex(
                name: "IX_Customer_StoreId",
                table: "Customer");

            migrationBuilder.DropIndex(
                name: "IX_Component_StoreId",
                table: "Component");

            migrationBuilder.DropIndex(
                name: "IX_Charge_StoreId",
                table: "Charge");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "ShopSetting");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ServiceTicket");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "ServiceTicket");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ServiceTicket");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Service");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "MetaFieldDefinition");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Mechanic");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Component");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Charge");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Charge");

            migrationBuilder.CreateIndex(
                name: "IX_ShopSetting_Key",
                table: "ShopSetting",
                column: "Key",
                unique: true);
        }
    }
}
