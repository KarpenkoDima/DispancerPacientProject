using DispancerPacient.Application.Doctors.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispancerPacient.Web.Pages.Doctors;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator) => _mediator = mediator;

    public IList<DoctorListItem> Doctors { get; set; } = [];

    public async Task OnGetAsync()
    {
        Doctors = await _mediator.Send(new GetDoctorsQuery());
    }
}
