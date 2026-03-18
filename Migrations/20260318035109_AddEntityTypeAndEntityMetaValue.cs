using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikePOS.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityTypeAndEntityMetaValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "MetaFieldDefinition",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "EntityMetaValue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    MetaFieldDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_MetaFieldDefinition_EntityType_Key_StoreId",
                table: "MetaFieldDefinition",
                columns: new[] { "EntityType", "Key", "StoreId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntityMetaValue_EntityType_EntityId_MetaFieldDefinitionId",
                table: "EntityMetaValue",
                columns: new[] { "EntityType", "EntityId", "MetaFieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntityMetaValue_MetaFieldDefinitionId",
                table: "EntityMetaValue",
                column: "MetaFieldDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntityMetaValue");

            migrationBuilder.DropIndex(
                name: "IX_MetaFieldDefinition_EntityType_Key_StoreId",
                table: "MetaFieldDefinition");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "MetaFieldDefinition");
        }
    }
}
