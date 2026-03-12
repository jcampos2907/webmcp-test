using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikePOS.Migrations
{
    /// <inheritdoc />
    public partial class AddMetaFieldValidationAndConditionals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConditionalOnFieldId",
                table: "MetaFieldDefinition",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConditionalOnValue",
                table: "MetaFieldDefinition",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormatMask",
                table: "MetaFieldDefinition",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Options",
                table: "MetaFieldDefinition",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegexMessage",
                table: "MetaFieldDefinition",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegexPattern",
                table: "MetaFieldDefinition",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetaFieldDefinition_ConditionalOnFieldId",
                table: "MetaFieldDefinition",
                column: "ConditionalOnFieldId");

            migrationBuilder.AddForeignKey(
                name: "FK_MetaFieldDefinition_MetaFieldDefinition_ConditionalOnFieldId",
                table: "MetaFieldDefinition",
                column: "ConditionalOnFieldId",
                principalTable: "MetaFieldDefinition",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MetaFieldDefinition_MetaFieldDefinition_ConditionalOnFieldId",
                table: "MetaFieldDefinition");

            migrationBuilder.DropIndex(
                name: "IX_MetaFieldDefinition_ConditionalOnFieldId",
                table: "MetaFieldDefinition");

            migrationBuilder.DropColumn(
                name: "ConditionalOnFieldId",
                table: "MetaFieldDefinition");

            migrationBuilder.DropColumn(
                name: "ConditionalOnValue",
                table: "MetaFieldDefinition");

            migrationBuilder.DropColumn(
                name: "FormatMask",
                table: "MetaFieldDefinition");

            migrationBuilder.DropColumn(
                name: "Options",
                table: "MetaFieldDefinition");

            migrationBuilder.DropColumn(
                name: "RegexMessage",
                table: "MetaFieldDefinition");

            migrationBuilder.DropColumn(
                name: "RegexPattern",
                table: "MetaFieldDefinition");
        }
    }
}
