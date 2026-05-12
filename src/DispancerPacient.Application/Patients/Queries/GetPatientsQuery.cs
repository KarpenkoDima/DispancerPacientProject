using DispancerPacient.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DispancerPacient.Application.Patients.Queries;

public record GetPatientsQuery(string? SearchTerm, bool OnlyOnRecord) : IRequest<IList<PatientListItem>>;

public class PatientListItem
{
    public int Id { get; init; }
    public string FullName { get; init; } = null!;
    public DateTime BirthDate { get; init; }
    public string? MedicalRecordNumber { get; init; }
    public string? DiagnosisCode { get; init; }
    public string? DoctorName { get; init; }
    public bool IsOnRecord { get; init; }
}

public class GetPatientsQueryHandler : IRequestHandler<GetPatientsQuery, IList<PatientListItem>>
{
    private readonly IAppDbContext _db;

    public GetPatientsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IList<PatientListItem>> Handle(GetPatientsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Patients.Include(p => p.Doctor).AsQueryable();

        if (request.OnlyOnRecord)
            query = query.Where(p => p.IsOnRecord);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(p =>
                p.LastName.ToLower().Contains(term) ||
                p.FirstName.ToLower().Contains(term) ||
                (p.IpnCode != null && p.IpnCode.Contains(term)) ||
                (p.MedicalRecordNumber != null && p.MedicalRecordNumber.Contains(term)));
        }

        return await query
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Select(p => new PatientListItem
            {
                Id = p.Id,
                FullName = p.FullName,
                BirthDate = p.BirthDate,
                MedicalRecordNumber = p.MedicalRecordNumber,
                DiagnosisCode = p.DiagnosisCode,
                DoctorName = p.Doctor != null ? p.Doctor.LastName + " " + p.Doctor.FirstName : null,
                IsOnRecord = p.IsOnRecord
            })
            .ToListAsync(cancellationToken);
    }
}
