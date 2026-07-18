using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedMinimalApi.Migrations
{
    /// <inheritdoc />
    public partial class AddClasseTerapeuticaMedicamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "QtdeDisponivel",
                table: "Medicamentos",
                newName: "ClasseTerapeutica");

            migrationBuilder.AddColumn<bool>(
                name: "FornecidoPeloSUS",
                table: "Medicamentos",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FornecidoPeloSUS",
                table: "Medicamentos");

            migrationBuilder.RenameColumn(
                name: "ClasseTerapeutica",
                table: "Medicamentos",
                newName: "QtdeDisponivel");
        }
    }
}
