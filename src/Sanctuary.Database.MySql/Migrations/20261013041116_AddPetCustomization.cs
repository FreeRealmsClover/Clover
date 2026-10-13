using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanctuary.Database.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddPetCustomization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The live database already has IsLead (added by hand before this migration existed).
            migrationBuilder.Sql("ALTER TABLE `Users` ADD COLUMN IF NOT EXISTS `IsLead` tinyint(1) NOT NULL DEFAULT 0;");

            migrationBuilder.AddColumn<string>(
                name: "PetClothing",
                table: "Items",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PetName",
                table: "Items",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLead",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PetClothing",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "PetName",
                table: "Items");
        }
    }
}
