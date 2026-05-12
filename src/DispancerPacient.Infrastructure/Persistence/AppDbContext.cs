using DispancerPacient.Application.Interfaces;
using DispancerPacient.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DispancerPacient.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Явно указываем backing fields для коллекций с IReadOnlyCollection
        modelBuilder.Entity<Doctor>()
            .Navigation(d => d.Patients)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.Entity<Patient>()
            .Navigation(p => p.Appointments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Убираем теневые навигации, которые EF мог создать по конвенции
        modelBuilder.Entity<Doctor>().Ignore("Appointments");
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
