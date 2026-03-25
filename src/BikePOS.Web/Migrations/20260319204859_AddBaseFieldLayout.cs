using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikePOS.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseFieldLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BaseFieldLayout",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FieldKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CompanyId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    StoreId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseFieldLayout", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaseFieldLayout_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BaseFieldLayout_Store_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Store",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaseFieldLayout_CompanyId",
                table: "BaseFieldLayout",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseFieldLayout_EntityType_FieldKey_CompanyId",
                table: "BaseFieldLayout",
                columns: new[] { "EntityType", "FieldKey", "CompanyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BaseFieldLayout_StoreId",
                table: "BaseFieldLayout",
                column: "StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaseFieldLayout");
        }
    }
}
