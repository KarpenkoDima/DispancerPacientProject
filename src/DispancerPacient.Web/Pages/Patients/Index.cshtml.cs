using DispancerPacient.Application.Patients.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispancerPacient.Web.Pages.Patients;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator) => _mediator = mediator;

    public IList<PatientListItem> Patients { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OnlyOnRecord { get; set; } = true;

    public async Task OnGetAsync()
    {
        Patients = await _mediator.Send(new GetPatientsQuery(SearchTerm, OnlyOnRecord));
    }
}
