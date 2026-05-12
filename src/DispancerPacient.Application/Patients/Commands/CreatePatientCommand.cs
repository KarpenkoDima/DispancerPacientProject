using DispancerPacient.Application.Interfaces;
using DispancerPacient.Domain.Entities;
using DispancerPacient.Domain.Enums;
using DispancerPacient.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DispancerPacient.Application.Patients.Commands;

public record CreatePatientCommand(
    string LastName, string FirstName, string? MiddleName,
    DateTime BirthDate, Gender Gender, string? IpnCode,
    string? Address, string? Phone,
    string? DiagnosisCode, string? DiagnosisDescription,
    int? DoctorId, bool IsOnRecord, DateTime? RegistrationDate
) : IRequest<Result<int>>;

public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, Result<int>>
{
    private readonly IAppDbContext _db;

    public CreatePatientCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<int>> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.IpnCode))
        {
            var exists = await _db.Patients.AnyAsync(
                p => p.IpnCode == request.IpnCode, cancellationToken);
            if (exists)
                return Result.Failure<int>("Пациент с таким ИНН уже существует.");
        }

        var patient = Patient.Create(
            request.LastName, request.FirstName, request.MiddleName,
            request.BirthDate, request.Gender, request.IpnCode,
            request.Address, request.Phone,
            request.DiagnosisCode, request.DiagnosisDescription,
            request.DoctorId, request.IsOnRecord, request.RegistrationDate);

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(cancellationToken);

        // Генерируем номер медицинской карты
        patient.SetMedicalRecordNumber($"МК-{patient.Id:D6}");
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(patient.Id);
    }
}
