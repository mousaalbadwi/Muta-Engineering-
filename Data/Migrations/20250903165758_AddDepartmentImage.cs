using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MutaEngineering.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Departments",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Departments");
        }
    }
}
