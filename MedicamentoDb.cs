using Microsoft.EntityFrameworkCore;

public class MedicamentoDb : DbContext
{
    public MedicamentoDb(DbContextOptions<MedicamentoDb> options) : base(options) { }

    public DbSet<Medicamento> Medicamentos => Set<Medicamento>();
    public DbSet<Apresentacao> Apresentacoes => Set<Apresentacao>();
    public DbSet<FarmaciaMunicipal> FarmaciasMunicipais => Set<FarmaciaMunicipal>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Medicamento>()
            .HasMany(m => m.Apresentacoes)
            .WithOne(a => a.Medicamento)
            .HasForeignKey(a => a.MedicamentoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FarmaciaMunicipal>()
            .HasIndex(f => f.Nome)
            .IsUnique();

        modelBuilder.Entity<FarmaciaMunicipal>()
            .HasIndex(f => f.Slug)
            .IsUnique();

        modelBuilder.Entity<Apresentacao>()
            .HasMany(a => a.Unidades)
            .WithMany(f => f.Apresentacoes)
            .UsingEntity(j => j.ToTable("ApresentacaoFarmacias"));
    }
}
