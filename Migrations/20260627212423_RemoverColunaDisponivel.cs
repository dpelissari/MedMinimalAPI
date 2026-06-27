using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedMinimalApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoverColunaDisponivel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Disponivel",
                table: "Medicamentos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Disponivel",
                table: "Medicamentos",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
