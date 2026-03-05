namespace DispancerPacient.Domain.Entities;

public class Doctor : BaseEntity
{
    public string LastName { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string? MiddleName { get; private set; }
    public string Specialization { get; private set; } = null!;
    public string? LicenseNumber { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<Patient> _patients = [];
    public IReadOnlyCollection<Patient> Patients => _patients.AsReadOnly();

    public string FullName => MiddleName is not null
        ? $"{LastName} {FirstName} {MiddleName}"
        : $"{LastName} {FirstName}";

    private Doctor() { }

    public static Doctor Create(
        string lastName, string firstName, string? middleName,
        string specialization, string? licenseNumber,
        string? phone, string? email)
    {
        return new Doctor
        {
            LastName = lastName,
            FirstName = firstName,
            MiddleName = middleName,
            Specialization = specialization,
            LicenseNumber = licenseNumber,
            Phone = phone,
            Email = email,
            IsActive = true
        };
    }

    public void Update(
        string lastName, string firstName, string? middleName,
        string specialization, string? licenseNumber,
        string? phone, string? email, bool isActive)
    {
        LastName = lastName;
        FirstName = firstName;
        MiddleName = middleName;
        Specialization = specialization;
        LicenseNumber = licenseNumber;
        Phone = phone;
        Email = email;
        IsActive = isActive;
    }
}
