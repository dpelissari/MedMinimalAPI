using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedMinimalApi.Migrations
{
    /// <inheritdoc />
    public partial class UsuarioPerfilEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Converte os textos antigos para 10 e 20 e muda a coluna para integer
            migrationBuilder.Sql(@"
                ALTER TABLE ""Usuarios"" 
                ALTER COLUMN ""Perfil"" TYPE integer 
                USING (
                    CASE ""Perfil""
                        WHEN 'Administrador' THEN 10
                        WHEN 'Funcionario' THEN 20
                        ELSE 20 
                    END
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Caminho de volta: Converte de 10/20 para texto se precisar desfazer a migration
            migrationBuilder.Sql(@"
                ALTER TABLE ""Usuarios"" 
                ALTER COLUMN ""Perfil"" TYPE text 
                USING (
                    CASE ""Perfil""
                        WHEN 10 THEN 'Administrador'
                        WHEN 20 THEN 'Funcionario'
                        ELSE 'Funcionario' 
                    END
                );
            ");
        }
    }
}
