using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiraRemumeApi.Migrations
{
    /// <inheritdoc />
    public partial class DadosFarmaciasMunicipais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "Aliases",
                table: "FarmaciasMunicipais",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "Bairro",
                table: "FarmaciasMunicipais",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Endereco",
                table: "FarmaciasMunicipais",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Horario",
                table: "FarmaciasMunicipais",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Observacao",
                table: "FarmaciasMunicipais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "FarmaciasMunicipais",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Telefone",
                table: "FarmaciasMunicipais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "FarmaciasMunicipais",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string[]>(
                name: "UnidadesReferencia",
                table: "FarmaciasMunicipais",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<bool>(
                name: "Whatsapp",
                table: "FarmaciasMunicipais",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ZonaRural",
                table: "FarmaciasMunicipais",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE "FarmaciasMunicipais"
                SET "Slug" = "Id"::text
                WHERE "Slug" IS NULL OR "Slug" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_FarmaciasMunicipais_Slug",
                table: "FarmaciasMunicipais",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FarmaciasMunicipais_Slug",
                table: "FarmaciasMunicipais");

            migrationBuilder.DropColumn(
                name: "Aliases",
                table: "FarmaciasMunicipais");

            migrationBuilder.DropColumn(
                name: "Bairro",
                table: "FarmaciasMunicipais");

            migrationBuilder.DropColumn(
                name: "Endereco",
                table: "FarmaciasMunicipais");

            migrationBuilder.DropColumn(
                name: "Horario",
                table: "FarmaciasMunicipais");

            migrationBuilder.DropColumn(
                name: "Observacao",
                table: "FarmaciasMunicipais");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "FarmaciasMunicipais");

            migrationBuilder.DropColumn(
                name: "Telefone",
                table: "FarmaciasMunicipais");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "FarmaciasMunicipais");

            migrationBuilder.DropColumn(
                name: "UnidadesReferencia",
                table: "FarmaciasMunicipais");

            migrationBuilder.DropColumn(
                name: "Whatsapp",
                table: "FarmaciasMunicipais");

            migrationBuilder.DropColumn(
                name: "ZonaRural",
                table: "FarmaciasMunicipais");
        }
    }
}
