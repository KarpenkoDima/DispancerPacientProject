using DispancerPacient.Domain.Enums;
using DispancerPacient.Domain.Events;

namespace DispancerPacient.Domain.Entities;

public class Patient : BaseEntity
{
    public string LastName { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string? MiddleName { get; private set; }
    public DateTime BirthDate { get; private set; }
    public Gender Gender { get; private set; }
    public string? IpnCode { get; private set; }
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public string? MedicalRecordNumber { get; private set; }
    public string? DiagnosisCode { get; private set; }
    public string? DiagnosisDescription { get; private set; }
    public int? DoctorId { get; private set; }
    public Doctor? Doctor { get; private set; }
    public bool IsOnRecord { get; private set; }
    public DateTime? RegistrationDate { get; private set; }
    public DateTime? DeregistrationDate { get; private set; }

    private readonly List<Appointment> _appointments = [];
    public IReadOnlyCollection<Appointment> Appointments => _appointments.AsReadOnly();

    public string FullName => MiddleName is not null
        ? $"{LastName} {FirstName} {MiddleName}"
        : $"{LastName} {FirstName}";

    private Patient() { }

    public static Patient Create(
        string lastName, string firstName, string? middleName,
        DateTime birthDate, Gender gender, string? ipnCode,
        string? address, string? phone,
        string? diagnosisCode, string? diagnosisDescription,
        int? doctorId, bool isOnRecord, DateTime? registrationDate)
    {
        var patient = new Patient
        {
            LastName = lastName,
            FirstName = firstName,
            MiddleName = middleName,
            BirthDate = birthDate,
            Gender = gender,
            IpnCode = ipnCode,
            Address = address,
            Phone = phone,
            DiagnosisCode = diagnosisCode,
            DiagnosisDescription = diagnosisDescription,
            DoctorId = doctorId,
            IsOnRecord = isOnRecord,
            RegistrationDate = registrationDate
        };

        patient.AddDomainEvent(new PatientCreatedEvent(0, patient.FullName));
        return patient;
    }

    public void Update(
        string lastName, string firstName, string? middleName,
        DateTime birthDate, Gender gender, string? ipnCode,
        string? address, string? phone,
        string? diagnosisCode, string? diagnosisDescription,
        int? doctorId, bool isOnRecord,
        DateTime? registrationDate, DateTime? deregistrationDate)
    {
        var oldDiagnosis = DiagnosisCode;
        LastName = lastName;
        FirstName = firstName;
        MiddleName = middleName;
        BirthDate = birthDate;
        Gender = gender;
        IpnCode = ipnCode;
        Address = address;
        Phone = phone;
        DiagnosisCode = diagnosisCode;
        DiagnosisDescription = diagnosisDescription;
        DoctorId = doctorId;
        IsOnRecord = isOnRecord;
        RegistrationDate = registrationDate;
        DeregistrationDate = deregistrationDate;

        if (oldDiagnosis != diagnosisCode)
            AddDomainEvent(new DiagnosisChangedEvent(Id, oldDiagnosis, diagnosisCode));
    }

    public void SetMedicalRecordNumber(string number) => MedicalRecordNumber = number;
}
