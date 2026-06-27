using Microsoft.EntityFrameworkCore;

public class MedicamentoDb : DbContext
{
    public MedicamentoDb(DbContextOptions<MedicamentoDb> options) : base(options) { }

    public DbSet<Medicamento> Medicamentos => Set<Medicamento>();
    public DbSet<Fabricante> Fabricantes => Set<Fabricante>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Diz ao EF para nunca criar uma coluna para essa propriedade
        modelBuilder.Entity<Medicamento>().Ignore(m => m.Disponivel);
    }
}