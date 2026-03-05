using DispancerPacient.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DispancerPacient.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.LastName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.MiddleName).HasMaxLength(100);
        builder.Property(p => p.IpnCode).HasMaxLength(10);
        builder.Property(p => p.Address).HasMaxLength(500);
        builder.Property(p => p.Phone).HasMaxLength(20);
        builder.Property(p => p.MedicalRecordNumber).HasMaxLength(20);
        builder.Property(p => p.DiagnosisCode).HasMaxLength(10);
        builder.Property(p => p.DiagnosisDescription).HasMaxLength(1000);

        builder.HasIndex(p => p.IpnCode).IsUnique().HasFilter("[IpnCode] IS NOT NULL");
        builder.HasIndex(p => p.MedicalRecordNumber).IsUnique().HasFilter("[MedicalRecordNumber] IS NOT NULL");
        builder.HasIndex(p => p.LastName);

        builder.HasOne(p => p.Doctor)
            .WithMany(d => d.Patients)
            .HasForeignKey(p => p.DoctorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Игнорируем вычисляемое свойство FullName
        builder.Ignore(p => p.FullName);
    }
}
