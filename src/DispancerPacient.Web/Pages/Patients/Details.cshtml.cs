using DispancerPacient.Application.Patients.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispancerPacient.Web.Pages.Patients;

public class DetailsModel : PageModel
{
    private readonly IMediator _mediator;

    public DetailsModel(IMediator mediator) => _mediator = mediator;

    public PatientDetailItem Patient { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var result = await _mediator.Send(new GetPatientByIdQuery(id));

        if (result.IsFailure)
            return NotFound();

        Patient = result.Value!;
        return Page();
    }
}
