using DispancerPacient.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DispancerPacient.Application.Doctors.Queries;

public record GetDoctorsQuery() : IRequest<IList<DoctorListItem>>;

public class DoctorListItem
{
    public int Id { get; init; }
    public string FullName { get; init; } = null!;
    public string Specialization { get; init; } = null!;
    public string? LicenseNumber { get; init; }
    public int PatientCount { get; init; }
    public bool IsActive { get; init; }
}

public class GetDoctorsQueryHandler : IRequestHandler<GetDoctorsQuery, IList<DoctorListItem>>
{
    private readonly IAppDbContext _db;

    public GetDoctorsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IList<DoctorListItem>> Handle(GetDoctorsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Doctors
            .OrderBy(d => d.LastName)
            .Select(d => new DoctorListItem
            {
                Id = d.Id,
                FullName = d.FullName,
                Specialization = d.Specialization,
                LicenseNumber = d.LicenseNumber,
                PatientCount = d.Patients.Count(p => p.IsOnRecord),
                IsActive = d.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}
