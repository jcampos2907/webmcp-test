using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikePOS.Migrations
{
    /// <inheritdoc />
    public partial class InitialUuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUser",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
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
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
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
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ConglomerateId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Locale = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    TaxId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CountryCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
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
                name: "OidcConfig",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ConglomerateId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Authority = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ClientSecret = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ResponseType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Scopes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    MapInboundClaims = table.Column<bool>(type: "INTEGER", nullable: false),
                    SaveTokens = table.Column<bool>(type: "INTEGER", nullable: false),
                    GetClaimsFromUserInfoEndpoint = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProviderName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OidcConfig", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OidcConfig_Conglomerate_ConglomerateId",
                        column: x => x.ConglomerateId,
                        principalTable: "Conglomerate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Store",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CompanyId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
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
                name: "Customer",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Street = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ZipCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StoreId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customer_Store_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Store",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Mechanic",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    StoreId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mechanic", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mechanic_Store_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Store",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MetaFieldDefinition",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    FieldType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    RegexPattern = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RegexMessage = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    FormatMask = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Options = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ConditionalOnFieldId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    ConditionalOnValue = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CompanyId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    StoreId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaFieldDefinition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetaFieldDefinition_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetaFieldDefinition_MetaFieldDefinition_ConditionalOnFieldId",
                        column: x => x.ConditionalOnFieldId,
                        principalTable: "MetaFieldDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MetaFieldDefinition_Store_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Store",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PaymentTerminal",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    StoreId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTerminal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTerminal_Store_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Store",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    QuantityInStock = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    StoreId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Product_Store_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Store",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Service",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultPrice = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    StoreId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Service", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Service_Store_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Store",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShopSetting",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    StoreId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopSetting_Store_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Store",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StoreUser",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    AppUserId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    StoreId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
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

            migrationBuilder.CreateTable(
                name: "Component",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: false),
                    Brand = table.Column<string>(type: "TEXT", nullable: false),
                    ComponentType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    CustomerId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    StoreId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Component", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Component_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Component_Store_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Store",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerMetaValue",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    MetaFieldDefinitionId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerMetaValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerMetaValue_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerMetaValue_MetaFieldDefinition_MetaFieldDefinitionId",
                        column: x => x.MetaFieldDefinitionId,
                        principalTable: "MetaFieldDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EntityMetaValue",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    MetaFieldDefinitionId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityMetaValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityMetaValue_MetaFieldDefinition_MetaFieldDefinitionId",
                        column: x => x.MetaFieldDefinitionId,
                        principalTable: "MetaFieldDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceTicket",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    TicketNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ComponentId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    MechanicId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    BaseServiceId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    StoreId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTicket", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceTicket_Component_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Component",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceTicket_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServiceTicket_Mechanic_MechanicId",
                        column: x => x.MechanicId,
                        principalTable: "Mechanic",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServiceTicket_Service_BaseServiceId",
                        column: x => x.BaseServiceId,
                        principalTable: "Service",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServiceTicket_Store_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Store",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Charge",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ServiceTicketId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    ChargedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CashierName = table.Column<string>(type: "TEXT", nullable: true),
                    PaymentMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalTransactionId = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    PaymentStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StoreId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Charge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Charge_ServiceTicket_ServiceTicketId",
                        column: x => x.ServiceTicketId,
                        principalTable: "ServiceTicket",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Charge_Store_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Store",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TicketProduct",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ServiceTicketId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ProductId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
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

            migrationBuilder.CreateTable(
                name: "PaymentSession",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ChargeId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    TerminalId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    ExternalRef = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentSession_Charge_ChargeId",
                        column: x => x.ChargeId,
                        principalTable: "Charge",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentSession_PaymentTerminal_TerminalId",
                        column: x => x.TerminalId,
                        principalTable: "PaymentTerminal",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUser_ExternalSubjectId",
                table: "AppUser",
                column: "ExternalSubjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Charge_ServiceTicketId",
                table: "Charge",
                column: "ServiceTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Charge_StoreId",
                table: "Charge",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Company_ConglomerateId",
                table: "Company",
                column: "ConglomerateId");

            migrationBuilder.CreateIndex(
                name: "IX_Component_CustomerId",
                table: "Component",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Component_StoreId",
                table: "Component",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_StoreId",
                table: "Customer",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerMetaValue_CustomerId_MetaFieldDefinitionId",
                table: "CustomerMetaValue",
                columns: new[] { "CustomerId", "MetaFieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerMetaValue_MetaFieldDefinitionId",
                table: "CustomerMetaValue",
                column: "MetaFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityMetaValue_EntityType_EntityId_MetaFieldDefinitionId",
                table: "EntityMetaValue",
                columns: new[] { "EntityType", "EntityId", "MetaFieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntityMetaValue_MetaFieldDefinitionId",
                table: "EntityMetaValue",
                column: "MetaFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Mechanic_StoreId",
                table: "Mechanic",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaFieldDefinition_CompanyId",
                table: "MetaFieldDefinition",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaFieldDefinition_ConditionalOnFieldId",
                table: "MetaFieldDefinition",
                column: "ConditionalOnFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaFieldDefinition_EntityType_Key_CompanyId",
                table: "MetaFieldDefinition",
                columns: new[] { "EntityType", "Key", "CompanyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetaFieldDefinition_StoreId",
                table: "MetaFieldDefinition",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_OidcConfig_ConglomerateId_IsActive",
                table: "OidcConfig",
                columns: new[] { "ConglomerateId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSession_ChargeId",
                table: "PaymentSession",
                column: "ChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSession_Status",
                table: "PaymentSession",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSession_TerminalId",
                table: "PaymentSession",
                column: "TerminalId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTerminal_StoreId_IsActive",
                table: "PaymentTerminal",
                columns: new[] { "StoreId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Product_StoreId",
                table: "Product",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Service_StoreId",
                table: "Service",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTicket_BaseServiceId",
                table: "ServiceTicket",
                column: "BaseServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTicket_ComponentId",
                table: "ServiceTicket",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTicket_CustomerId",
                table: "ServiceTicket",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTicket_MechanicId",
                table: "ServiceTicket",
                column: "MechanicId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTicket_StoreId_TicketNumber",
                table: "ServiceTicket",
                columns: new[] { "StoreId", "TicketNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopSetting_StoreId_Key",
                table: "ShopSetting",
                columns: new[] { "StoreId", "Key" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_TicketProduct_ProductId",
                table: "TicketProduct",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketProduct_ServiceTicketId_ProductId",
                table: "TicketProduct",
                columns: new[] { "ServiceTicketId", "ProductId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerMetaValue");

            migrationBuilder.DropTable(
                name: "EntityMetaValue");

            migrationBuilder.DropTable(
                name: "OidcConfig");

            migrationBuilder.DropTable(
                name: "PaymentSession");

            migrationBuilder.DropTable(
                name: "ShopSetting");

            migrationBuilder.DropTable(
                name: "StoreUser");

            migrationBuilder.DropTable(
                name: "TicketProduct");

            migrationBuilder.DropTable(
                name: "MetaFieldDefinition");

            migrationBuilder.DropTable(
                name: "Charge");

            migrationBuilder.DropTable(
                name: "PaymentTerminal");

            migrationBuilder.DropTable(
                name: "AppUser");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "ServiceTicket");

            migrationBuilder.DropTable(
                name: "Component");

            migrationBuilder.DropTable(
                name: "Mechanic");

            migrationBuilder.DropTable(
                name: "Service");

            migrationBuilder.DropTable(
                name: "Customer");

            migrationBuilder.DropTable(
                name: "Store");

            migrationBuilder.DropTable(
                name: "Company");

            migrationBuilder.DropTable(
                name: "Conglomerate");
        }
    }
}
