using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikePOS.Migrations
{
    /// <inheritdoc />
    public partial class AddOidcConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OidcConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConglomerateId = table.Column<int>(type: "INTEGER", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_OidcConfig_ConglomerateId_IsActive",
                table: "OidcConfig",
                columns: new[] { "ConglomerateId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OidcConfig");
        }
    }
}
