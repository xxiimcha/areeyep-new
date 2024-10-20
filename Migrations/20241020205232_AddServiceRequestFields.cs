using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AreEyeP.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRequestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ServiceRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Staff",
                table: "ServiceRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ServiceRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ServiceRequests",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "Staff",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ServiceRequests");
        }
    }
}
