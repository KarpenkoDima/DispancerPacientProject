using DispancerPacient.Application.Doctors.Queries;
using DispancerPacient.Application.Patients.Commands;
using DispancerPacient.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DispancerPacient.Web.Pages.Patients;

public class CreateModel : PageModel
{
    private readonly IMediator _mediator;

    public CreateModel(IMediator mediator) => _mediator = mediator;

    [BindProperty]
    public PatientFormModel Input { get; set; } = new();

    public SelectList DoctorsList { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadDoctorsAsync();
        Input.RegistrationDate = DateTime.Today;
        Input.IsOnRecord = true;
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
            var command = new CreatePatientCommand(
                Input.LastName, Input.FirstName, Input.MiddleName,
                Input.BirthDate, Input.Gender, Input.IpnCode,
                Input.Address, Input.Phone,
                Input.DiagnosisCode, Input.DiagnosisDescription,
                Input.DoctorId, Input.IsOnRecord, Input.RegistrationDate);

            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                await LoadDoctorsAsync();
                return Page();
            }

            return RedirectToPage("Details", new { id = result.Value });
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

public class PatientFormModel
{
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public DateTime BirthDate { get; set; }
    public Gender Gender { get; set; }
    public string? IpnCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? DiagnosisCode { get; set; }
    public string? DiagnosisDescription { get; set; }
    public int? DoctorId { get; set; }
    public bool IsOnRecord { get; set; } = true;
    public DateTime? RegistrationDate { get; set; }
    public DateTime? DeregistrationDate { get; set; }
}
