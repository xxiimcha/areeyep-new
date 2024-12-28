using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AreEyeP.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentProofToClientPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentProof",
                table: "ClientPayments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentProof",
                table: "ClientPayments");
        }
    }
}
