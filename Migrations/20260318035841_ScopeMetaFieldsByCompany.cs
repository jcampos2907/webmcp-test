using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikePOS.Migrations
{
    /// <inheritdoc />
    public partial class ScopeMetaFieldsByCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MetaFieldDefinition_EntityType_Key_StoreId",
                table: "MetaFieldDefinition");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "MetaFieldDefinition",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetaFieldDefinition_CompanyId",
                table: "MetaFieldDefinition",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaFieldDefinition_EntityType_Key_CompanyId",
                table: "MetaFieldDefinition",
                columns: new[] { "EntityType", "Key", "CompanyId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MetaFieldDefinition_Company_CompanyId",
                table: "MetaFieldDefinition",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MetaFieldDefinition_Company_CompanyId",
                table: "MetaFieldDefinition");

            migrationBuilder.DropIndex(
                name: "IX_MetaFieldDefinition_CompanyId",
                table: "MetaFieldDefinition");

            migrationBuilder.DropIndex(
                name: "IX_MetaFieldDefinition_EntityType_Key_CompanyId",
                table: "MetaFieldDefinition");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "MetaFieldDefinition");

            migrationBuilder.CreateIndex(
                name: "IX_MetaFieldDefinition_EntityType_Key_StoreId",
                table: "MetaFieldDefinition",
                columns: new[] { "EntityType", "Key", "StoreId" },
                unique: true);
        }
    }
}
