using DispancerPacient.Application.Interfaces;
using DispancerPacient.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DispancerPacient.Application.Appointments.Queries;

public record GetAppointmentsQuery(DateTime? Date, int? DoctorId) : IRequest<IList<AppointmentListItem>>;

public class AppointmentListItem
{
    public int Id { get; init; }
    public DateTime AppointmentDate { get; init; }
    public string PatientName { get; init; } = null!;
    public string DoctorName { get; init; } = null!;
    public AppointmentType Type { get; init; }
    public AppointmentStatus Status { get; init; }
    public string? DiagnosisCode { get; init; }
    public int PatientId { get; init; }
}

public class GetAppointmentsQueryHandler : IRequestHandler<GetAppointmentsQuery, IList<AppointmentListItem>>
{
    private readonly IAppDbContext _db;

    public GetAppointmentsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IList<AppointmentListItem>> Handle(GetAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .AsQueryable();

        if (request.Date.HasValue)
            query = query.Where(a => a.AppointmentDate.Date == request.Date.Value.Date);

        if (request.DoctorId.HasValue)
            query = query.Where(a => a.DoctorId == request.DoctorId.Value);

        return await query
            .OrderByDescending(a => a.AppointmentDate)
            .Select(a => new AppointmentListItem
            {
                Id = a.Id,
                AppointmentDate = a.AppointmentDate,
                PatientName = a.Patient.LastName + " " + a.Patient.FirstName,
                DoctorName = a.Doctor.LastName + " " + a.Doctor.FirstName,
                Type = a.Type,
                Status = a.Status,
                DiagnosisCode = a.DiagnosisCode,
                PatientId = a.PatientId
            })
            .ToListAsync(cancellationToken);
    }
}
