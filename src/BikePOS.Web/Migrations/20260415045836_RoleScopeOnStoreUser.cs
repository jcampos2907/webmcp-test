using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikePOS.Migrations
{
    /// <inheritdoc />
    public partial class RoleScopeOnStoreUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoreUser_AppUserId_StoreId",
                table: "StoreUser");

            migrationBuilder.AlterColumn<string>(
                name: "StoreId",
                table: "StoreUser",
                type: "TEXT",
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 36);

            migrationBuilder.AddColumn<string>(
                name: "CompanyId",
                table: "StoreUser",
                type: "TEXT",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConglomerateId",
                table: "StoreUser",
                type: "TEXT",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "StoreUser",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StoreUser_AppUserId_Scope_StoreId_CompanyId_ConglomerateId",
                table: "StoreUser",
                columns: new[] { "AppUserId", "Scope", "StoreId", "CompanyId", "ConglomerateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreUser_CompanyId",
                table: "StoreUser",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreUser_ConglomerateId",
                table: "StoreUser",
                column: "ConglomerateId");

            migrationBuilder.AddForeignKey(
                name: "FK_StoreUser_Company_CompanyId",
                table: "StoreUser",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StoreUser_Conglomerate_ConglomerateId",
                table: "StoreUser",
                column: "ConglomerateId",
                principalTable: "Conglomerate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoreUser_Company_CompanyId",
                table: "StoreUser");

            migrationBuilder.DropForeignKey(
                name: "FK_StoreUser_Conglomerate_ConglomerateId",
                table: "StoreUser");

            migrationBuilder.DropIndex(
                name: "IX_StoreUser_AppUserId_Scope_StoreId_CompanyId_ConglomerateId",
                table: "StoreUser");

            migrationBuilder.DropIndex(
                name: "IX_StoreUser_CompanyId",
                table: "StoreUser");

            migrationBuilder.DropIndex(
                name: "IX_StoreUser_ConglomerateId",
                table: "StoreUser");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "StoreUser");

            migrationBuilder.DropColumn(
                name: "ConglomerateId",
                table: "StoreUser");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "StoreUser");

            migrationBuilder.AlterColumn<string>(
                name: "StoreId",
                table: "StoreUser",
                type: "TEXT",
                maxLength: 36,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreUser_AppUserId_StoreId",
                table: "StoreUser",
                columns: new[] { "AppUserId", "StoreId" },
                unique: true);
        }
    }
}
