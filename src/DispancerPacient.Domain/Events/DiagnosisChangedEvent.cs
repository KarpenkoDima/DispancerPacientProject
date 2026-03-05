namespace DispancerPacient.Domain.Events;

public record DiagnosisChangedEvent(int PatientId, string? OldDiagnosis, string? NewDiagnosis) : IDomainEvent;
