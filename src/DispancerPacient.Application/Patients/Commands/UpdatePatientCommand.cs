using DispancerPacient.Application.Interfaces;
using DispancerPacient.Domain.Enums;
using DispancerPacient.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DispancerPacient.Application.Patients.Commands;

public record UpdatePatientCommand(
    int Id,
    string LastName, string FirstName, string? MiddleName,
    DateTime BirthDate, Gender Gender, string? IpnCode,
    string? Address, string? Phone,
    string? DiagnosisCode, string? DiagnosisDescription,
    int? DoctorId, bool IsOnRecord,
    DateTime? RegistrationDate, DateTime? DeregistrationDate
) : IRequest<Result>;

public class UpdatePatientCommandHandler : IRequestHandler<UpdatePatientCommand, Result>
{
    private readonly IAppDbContext _db;

    public UpdatePatientCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(UpdatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(
            p => p.Id == request.Id, cancellationToken);

        if (patient is null)
            return Result.Failure("Пациент не найден.");

        if (!string.IsNullOrEmpty(request.IpnCode))
        {
            var duplicate = await _db.Patients.AnyAsync(
                p => p.IpnCode == request.IpnCode && p.Id != request.Id, cancellationToken);
            if (duplicate)
                return Result.Failure("Пациент с таким ИНН уже существует.");
        }

        patient.Update(
            request.LastName, request.FirstName, request.MiddleName,
            request.BirthDate, request.Gender, request.IpnCode,
            request.Address, request.Phone,
            request.DiagnosisCode, request.DiagnosisDescription,
            request.DoctorId, request.IsOnRecord,
            request.RegistrationDate, request.DeregistrationDate);

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
