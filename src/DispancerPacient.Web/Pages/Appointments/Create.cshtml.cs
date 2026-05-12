using DispancerPacient.Application.Appointments.Commands;
using DispancerPacient.Application.Doctors.Queries;
using DispancerPacient.Application.Interfaces;
using DispancerPacient.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DispancerPacient.Web.Pages.Appointments;

public class CreateModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IAppDbContext _db;

    public CreateModel(IMediator mediator, IAppDbContext db)
    {
        _mediator = mediator;
        _db = db;
    }

    [BindProperty]
    public AppointmentFormModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? PatientId { get; set; }

    public SelectList PatientsList { get; set; } = null!;
    public SelectList DoctorsList { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadListsAsync();
        Input.AppointmentDate = DateTime.Now;
        if (PatientId.HasValue)
            Input.PatientId = PatientId.Value;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return Page();
        }

        var command = new CreateAppointmentCommand(
            Input.PatientId, Input.DoctorId, Input.AppointmentDate,
            Input.Type, Input.DiagnosisCode, Input.Notes);

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await LoadListsAsync();
            return Page();
        }

        return RedirectToPage("Index");
    }

    private async Task LoadListsAsync()
    {
        var patients = await _db.Patients
            .Where(p => p.IsOnRecord)
            .Select(p => new { p.Id, Name = p.LastName + " " + p.FirstName })
            .OrderBy(p => p.Name)
            .ToListAsync();
        PatientsList = new SelectList(patients, "Id", "Name");

        var doctors = await _mediator.Send(new GetDoctorSelectListQuery());
        DoctorsList = new SelectList(doctors, "Id", "FullName");
    }
}

public class AppointmentFormModel
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public AppointmentType Type { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string? DiagnosisCode { get; set; }
    public string? Notes { get; set; }
}
