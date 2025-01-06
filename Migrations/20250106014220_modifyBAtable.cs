using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AreEyeP.Migrations
{
    /// <inheritdoc />
    public partial class modifyBAtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ForRenewal",
                table: "BurialApplications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRenewal",
                table: "BurialApplications",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ForRenewal",
                table: "BurialApplications");

            migrationBuilder.DropColumn(
                name: "IsRenewal",
                table: "BurialApplications");
        }
    }
}
