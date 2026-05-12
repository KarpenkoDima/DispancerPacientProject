namespace DispancerPacient.Domain.Events;

public record PatientCreatedEvent(int PatientId, string FullName) : IDomainEvent;
