using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AreEyeP.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBurialApplicationDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApplicantLastName",
                table: "BurialApplications",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "ApplicantFirstName",
                table: "BurialApplications",
                newName: "DeceasedLastName");

            migrationBuilder.AddColumn<string>(
                name: "DeceasedFirstName",
                table: "BurialApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeceasedFirstName",
                table: "BurialApplications");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "BurialApplications",
                newName: "ApplicantLastName");

            migrationBuilder.RenameColumn(
                name: "DeceasedLastName",
                table: "BurialApplications",
                newName: "ApplicantFirstName");
        }
    }
}
