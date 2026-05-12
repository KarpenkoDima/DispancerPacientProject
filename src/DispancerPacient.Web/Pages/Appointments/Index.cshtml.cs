using DispancerPacient.Application.Appointments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispancerPacient.Web.Pages.Appointments;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator) => _mediator = mediator;

    public IList<AppointmentListItem> Appointments { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public DateTime? FilterDate { get; set; }

    public async Task OnGetAsync()
    {
        Appointments = await _mediator.Send(new GetAppointmentsQuery(FilterDate, null));
    }
}
