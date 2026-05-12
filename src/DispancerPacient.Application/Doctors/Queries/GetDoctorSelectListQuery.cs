using DispancerPacient.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DispancerPacient.Application.Doctors.Queries;

public record GetDoctorSelectListQuery() : IRequest<IList<DoctorSelectItem>>;

public class DoctorSelectItem
{
    public int Id { get; init; }
    public string FullName { get; init; } = null!;
}

public class GetDoctorSelectListQueryHandler : IRequestHandler<GetDoctorSelectListQuery, IList<DoctorSelectItem>>
{
    private readonly IAppDbContext _db;

    public GetDoctorSelectListQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IList<DoctorSelectItem>> Handle(GetDoctorSelectListQuery request, CancellationToken cancellationToken)
    {
        return await _db.Doctors
            .Where(d => d.IsActive)
            .OrderBy(d => d.LastName)
            .Select(d => new DoctorSelectItem
            {
                Id = d.Id,
                FullName = d.FullName
            })
            .ToListAsync(cancellationToken);
    }
}
