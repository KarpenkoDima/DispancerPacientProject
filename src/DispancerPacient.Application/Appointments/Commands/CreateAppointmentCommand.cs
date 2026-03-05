using DispancerPacient.Application.Interfaces;
using DispancerPacient.Domain.Entities;
using DispancerPacient.Domain.Enums;
using DispancerPacient.Shared;
using MediatR;

namespace DispancerPacient.Application.Appointments.Commands;

public record CreateAppointmentCommand(
    int PatientId, int DoctorId,
    DateTime AppointmentDate, AppointmentType Type,
    string? DiagnosisCode, string? Notes
) : IRequest<Result<int>>;

public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, Result<int>>
{
    private readonly IAppDbContext _db;

    public CreateAppointmentCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<int>> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = Appointment.Create(
            request.PatientId, request.DoctorId,
            request.AppointmentDate, request.Type,
            request.DiagnosisCode, request.Notes);

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(appointment.Id);
    }
}
