using DispancerPacient.Application.Patients.Commands;
using DispancerPacient.Application.Patients.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispancerPacient.Web.Pages.Patients;

public class DeleteModel : PageModel
{
    private readonly IMediator _mediator;

    public DeleteModel(IMediator mediator) => _mediator = mediator;

    public PatientDetailItem Patient { get; set; } = null!;

    [BindProperty]
    public int PatientId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var result = await _mediator.Send(new GetPatientByIdQuery(id));
        if (result.IsFailure)
            return NotFound();

        Patient = result.Value!;
        PatientId = id;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _mediator.Send(new DeletePatientCommand(PatientId));
        return RedirectToPage("Index");
    }
}
