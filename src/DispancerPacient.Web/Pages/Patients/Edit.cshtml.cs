using DispancerPacient.Application.Doctors.Queries;
using DispancerPacient.Application.Patients.Commands;
using DispancerPacient.Application.Patients.Queries;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DispancerPacient.Web.Pages.Patients;

public class EditModel : PageModel
{
    private readonly IMediator _mediator;

    public EditModel(IMediator mediator) => _mediator = mediator;

    [BindProperty]
    public PatientFormModel Input { get; set; } = null!;

    [BindProperty]
    public int PatientId { get; set; }

    public SelectList DoctorsList { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var result = await _mediator.Send(new GetPatientByIdQuery(id));
        if (result.IsFailure)
            return NotFound();

        var p = result.Value!;
        PatientId = p.Id;
        Input = new PatientFormModel
        {
            LastName = p.LastName,
            FirstName = p.FirstName,
            MiddleName = p.MiddleName,
            BirthDate = p.BirthDate,
            Gender = p.Gender,
            IpnCode = p.IpnCode,
            Address = p.Address,
            Phone = p.Phone,
            DiagnosisCode = p.DiagnosisCode,
            DiagnosisDescription = p.DiagnosisDescription,
            DoctorId = p.DoctorId,
            IsOnRecord = p.IsOnRecord,
            RegistrationDate = p.RegistrationDate,
            DeregistrationDate = p.DeregistrationDate
        };

        await LoadDoctorsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDoctorsAsync();
            return Page();
        }

        try
        {
            var command = new UpdatePatientCommand(
                PatientId,
                Input.LastName, Input.FirstName, Input.MiddleName,
                Input.BirthDate, Input.Gender, Input.IpnCode,
                Input.Address, Input.Phone,
                Input.DiagnosisCode, Input.DiagnosisDescription,
                Input.DoctorId, Input.IsOnRecord,
                Input.RegistrationDate, Input.DeregistrationDate);

            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                await LoadDoctorsAsync();
                return Page();
            }

            return RedirectToPage("Details", new { id = PatientId });
        }
        catch (ValidationException ex)
        {
            foreach (var error in ex.Errors)
                ModelState.AddModelError($"Input.{error.PropertyName}", error.ErrorMessage);
            await LoadDoctorsAsync();
            return Page();
        }
    }

    private async Task LoadDoctorsAsync()
    {
        var doctors = await _mediator.Send(new GetDoctorSelectListQuery());
        DoctorsList = new SelectList(doctors, "Id", "FullName");
    }
}
