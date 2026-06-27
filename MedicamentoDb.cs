using Microsoft.EntityFrameworkCore;

public class MedicamentoDb : DbContext
{
    public MedicamentoDb(DbContextOptions<MedicamentoDb> options)
        : base(options) { }

    public DbSet<Medicamento> Medicamentos => Set<Medicamento>();
    public DbSet<Fabricante> Fabricantes => Set<Fabricante>();

}