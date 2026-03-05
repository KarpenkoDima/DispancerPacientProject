using DispancerPacient.Application.Interfaces;
using DispancerPacient.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DispancerPacient.Application.Patients.Commands;

public record DeletePatientCommand(int Id) : IRequest<Result>;

public class DeletePatientCommandHandler : IRequestHandler<DeletePatientCommand, Result>
{
    private readonly IAppDbContext _db;

    public DeletePatientCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(DeletePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .Include(p => p.Appointments)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (patient is null)
            return Result.Failure("Пациент не найден.");

        _db.Patients.Remove(patient);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
