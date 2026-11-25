using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiraRemumeApi.Migrations
{
    /// <inheritdoc />
    public partial class UrlFarmaciasSincronismo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UrlFarmacias",
                table: "ConfiguracoesSincronismo",
                type: "text",
                nullable: false,
                defaultValue: "https://piracicaba.sp.gov.br/servicos/farmacias-municipais/");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UrlFarmacias",
                table: "ConfiguracoesSincronismo");
        }
    }
}
