using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AreEyeP.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBurialApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "BurialApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApplicantFirstName",
                table: "BurialApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApplicantLastName",
                table: "BurialApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactInformation",
                table: "BurialApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RelationshipToDeceased",
                table: "BurialApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "BurialApplications");

            migrationBuilder.DropColumn(
                name: "ApplicantFirstName",
                table: "BurialApplications");

            migrationBuilder.DropColumn(
                name: "ApplicantLastName",
                table: "BurialApplications");

            migrationBuilder.DropColumn(
                name: "ContactInformation",
                table: "BurialApplications");

            migrationBuilder.DropColumn(
                name: "RelationshipToDeceased",
                table: "BurialApplications");
        }
    }
}
