using DispancerPacient.Application.Interfaces;
using DispancerPacient.Domain.Enums;
using DispancerPacient.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DispancerPacient.Application.Patients.Queries;

public record GetPatientByIdQuery(int Id) : IRequest<Result<PatientDetailItem>>;

public class PatientDetailItem
{
    public int Id { get; init; }
    public string FullName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string? MiddleName { get; init; }
    public DateTime BirthDate { get; init; }
    public Gender Gender { get; init; }
    public string? IpnCode { get; init; }
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? MedicalRecordNumber { get; init; }
    public string? DiagnosisCode { get; init; }
    public string? DiagnosisDescription { get; init; }
    public int? DoctorId { get; init; }
    public string? DoctorName { get; init; }
    public bool IsOnRecord { get; init; }
    public DateTime? RegistrationDate { get; init; }
    public DateTime? DeregistrationDate { get; init; }
    public List<AppointmentBriefItem> RecentAppointments { get; init; } = [];
}

public class AppointmentBriefItem
{
    public int Id { get; init; }
    public DateTime AppointmentDate { get; init; }
    public string TypeName { get; init; } = null!;
    public string DoctorName { get; init; } = null!;
    public string? DiagnosisCode { get; init; }
    public string StatusName { get; init; } = null!;
}

public class GetPatientByIdQueryHandler : IRequestHandler<GetPatientByIdQuery, Result<PatientDetailItem>>
{
    private readonly IAppDbContext _db;

    public GetPatientByIdQueryHandler(IAppDbContext db) => _db = db;

    public async Task<Result<PatientDetailItem>> Handle(GetPatientByIdQuery request, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .Include(p => p.Doctor)
            .Include(p => p.Appointments).ThenInclude(a => a.Doctor)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (patient is null)
            return Result.Failure<PatientDetailItem>("Пациент не найден.");

        var result = new PatientDetailItem
        {
            Id = patient.Id,
            FullName = patient.FullName,
            LastName = patient.LastName,
            FirstName = patient.FirstName,
            MiddleName = patient.MiddleName,
            BirthDate = patient.BirthDate,
            Gender = patient.Gender,
            IpnCode = patient.IpnCode,
            Address = patient.Address,
            Phone = patient.Phone,
            MedicalRecordNumber = patient.MedicalRecordNumber,
            DiagnosisCode = patient.DiagnosisCode,
            DiagnosisDescription = patient.DiagnosisDescription,
            DoctorId = patient.DoctorId,
            DoctorName = patient.Doctor?.FullName,
            IsOnRecord = patient.IsOnRecord,
            RegistrationDate = patient.RegistrationDate,
            DeregistrationDate = patient.DeregistrationDate,
            RecentAppointments = patient.Appointments
                .OrderByDescending(a => a.AppointmentDate)
                .Take(10)
                .Select(a => new AppointmentBriefItem
                {
                    Id = a.Id,
                    AppointmentDate = a.AppointmentDate,
                    TypeName = a.Type.ToString(),
                    DoctorName = a.Doctor.FullName,
                    DiagnosisCode = a.DiagnosisCode,
                    StatusName = a.Status.ToString()
                })
                .ToList()
        };

        return Result.Success(result);
    }
}
