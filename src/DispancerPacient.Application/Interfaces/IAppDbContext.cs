using DispancerPacient.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DispancerPacient.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Patient> Patients { get; }
    DbSet<Doctor> Doctors { get; }
    DbSet<Appointment> Appointments { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
