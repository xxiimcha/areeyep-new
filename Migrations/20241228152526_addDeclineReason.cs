using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AreEyeP.Migrations
{
    /// <inheritdoc />
    public partial class addDeclineReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeclineReason",
                table: "BurialApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeclineReason",
                table: "BurialApplications");
        }
    }
}
