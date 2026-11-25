using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiraRemumeApi.Migrations
{
    /// <inheritdoc />
    public partial class RemumePiracicaba : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Medicamentos_Fabricantes_FabricanteId",
                table: "Medicamentos");

            migrationBuilder.Sql("DELETE FROM \"Medicamentos\";");

            migrationBuilder.DropIndex(
                name: "IX_Medicamentos_FabricanteId",
                table: "Medicamentos");

            migrationBuilder.DropColumn(
                name: "ClasseTerapeutica",
                table: "Medicamentos");

            migrationBuilder.DropColumn(
                name: "FabricanteId",
                table: "Medicamentos");

            migrationBuilder.DropColumn(
                name: "FormaFarmaceutica",
                table: "Medicamentos");

            migrationBuilder.DropColumn(
                name: "FornecidoPeloSUS",
                table: "Medicamentos");

            migrationBuilder.CreateTable(
                name: "Apresentacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Abrangencia = table.Column<int>(type: "integer", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: true),
                    MedicamentoId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Apresentacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Apresentacoes_Medicamentos_MedicamentoId",
                        column: x => x.MedicamentoId,
                        principalTable: "Medicamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FarmaciasMunicipais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmaciasMunicipais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApresentacaoFarmacias",
                columns: table => new
                {
                    ApresentacoesId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnidadesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApresentacaoFarmacias", x => new { x.ApresentacoesId, x.UnidadesId });
                    table.ForeignKey(
                        name: "FK_ApresentacaoFarmacias_Apresentacoes_ApresentacoesId",
                        column: x => x.ApresentacoesId,
                        principalTable: "Apresentacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApresentacaoFarmacias_FarmaciasMunicipais_UnidadesId",
                        column: x => x.UnidadesId,
                        principalTable: "FarmaciasMunicipais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApresentacaoFarmacias_UnidadesId",
                table: "ApresentacaoFarmacias",
                column: "UnidadesId");

            migrationBuilder.CreateIndex(
                name: "IX_Apresentacoes_MedicamentoId",
                table: "Apresentacoes",
                column: "MedicamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmaciasMunicipais_Nome",
                table: "FarmaciasMunicipais",
                column: "Nome",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApresentacaoFarmacias");

            migrationBuilder.DropTable(
                name: "Apresentacoes");

            migrationBuilder.DropTable(
                name: "FarmaciasMunicipais");

            migrationBuilder.AddColumn<int>(
                name: "ClasseTerapeutica",
                table: "Medicamentos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "FabricanteId",
                table: "Medicamentos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "FormaFarmaceutica",
                table: "Medicamentos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "FornecidoPeloSUS",
                table: "Medicamentos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Medicamentos_FabricanteId",
                table: "Medicamentos",
                column: "FabricanteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Medicamentos_Fabricantes_FabricanteId",
                table: "Medicamentos",
                column: "FabricanteId",
                principalTable: "Fabricantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
