using DispancerPacient.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DispancerPacient.Infrastructure.Persistence.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.LastName).IsRequired().HasMaxLength(100);
        builder.Property(d => d.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(d => d.MiddleName).HasMaxLength(100);
        builder.Property(d => d.Specialization).IsRequired().HasMaxLength(200);
        builder.Property(d => d.LicenseNumber).HasMaxLength(50);
        builder.Property(d => d.Phone).HasMaxLength(20);
        builder.Property(d => d.Email).HasMaxLength(100);

        builder.Ignore(d => d.FullName);
        builder.Ignore(d => d.DomainEvents);

        // Начальные данные — 3 врача
        builder.HasData(
            new { Id = 1, LastName = "Иванов", FirstName = "Пётр", MiddleName = "Сергеевич", Specialization = "Психиатр", LicenseNumber = "ЛИЦ-001", Phone = "+380501234567", Email = "ivanov@dispanser.local", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null },
            new { Id = 2, LastName = "Петренко", FirstName = "Ольга", MiddleName = "Владимировна", Specialization = "Психотерапевт", LicenseNumber = "ЛИЦ-002", Phone = "+380509876543", Email = "petrenko@dispanser.local", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null },
            new { Id = 3, LastName = "Сидоров", FirstName = "Андрей", MiddleName = "Николаевич", Specialization = "Нарколог", LicenseNumber = "ЛИЦ-003", Phone = "+380507654321", Email = "sidorov@dispanser.local", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null }
        );
    }
}
