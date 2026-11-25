using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiraRemumeApi.Migrations
{
    /// <inheritdoc />
    public partial class ConfiguracaoSincronismo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracoesSincronismo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UrlFonte = table.Column<string>(type: "text", nullable: false),
                    IntervaloDias = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    UltimaVerificacaoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UltimoStatus = table.Column<string>(type: "text", nullable: true),
                    UltimaMensagem = table.Column<string>(type: "text", nullable: true),
                    UltimaAssinatura = table.Column<string>(type: "text", nullable: true),
                    LinksDetectados = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesSincronismo", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracoesSincronismo");
        }
    }
}
