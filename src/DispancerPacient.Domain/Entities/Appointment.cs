using DispancerPacient.Domain.Enums;

namespace DispancerPacient.Domain.Entities;

public class Appointment : BaseEntity
{
    public int PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    public int DoctorId { get; private set; }
    public Doctor Doctor { get; private set; } = null!;
    public DateTime AppointmentDate { get; private set; }
    public AppointmentType Type { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public string? DiagnosisCode { get; private set; }
    public string? Notes { get; private set; }

    private Appointment() { }

    public static Appointment Create(
        int patientId, int doctorId,
        DateTime appointmentDate, AppointmentType type,
        string? diagnosisCode, string? notes)
    {
        return new Appointment
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentDate = appointmentDate,
            Type = type,
            Status = AppointmentStatus.Scheduled,
            DiagnosisCode = diagnosisCode,
            Notes = notes
        };
    }

    public void Update(
        DateTime appointmentDate, AppointmentType type,
        AppointmentStatus status,
        string? diagnosisCode, string? notes)
    {
        AppointmentDate = appointmentDate;
        Type = type;
        Status = status;
        DiagnosisCode = diagnosisCode;
        Notes = notes;
    }
}
