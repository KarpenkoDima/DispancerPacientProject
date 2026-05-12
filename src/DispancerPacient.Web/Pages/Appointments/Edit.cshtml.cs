using DispancerPacient.Application.Doctors.Queries;
using DispancerPacient.Application.Interfaces;
using DispancerPacient.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DispancerPacient.Web.Pages.Appointments;

public class EditModel : PageModel
{
    private readonly IAppDbContext _db;
    private readonly IMediator _mediator;

    public EditModel(IAppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    [BindProperty]
    public AppointmentFormModel Input { get; set; } = null!;

    [BindProperty]
    public int AppointmentId { get; set; }

    public string PatientName { get; set; } = string.Empty;
    public SelectList DoctorsList { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var apt = await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (apt == null) return NotFound();

        AppointmentId = id;
        PatientName = apt.Patient.LastName + " " + apt.Patient.FirstName;
        Input = new AppointmentFormModel
        {
            PatientId = apt.PatientId,
            DoctorId = apt.DoctorId,
            AppointmentDate = apt.AppointmentDate,
            Type = apt.Type,
            Status = apt.Status,
            DiagnosisCode = apt.DiagnosisCode,
            Notes = apt.Notes
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

        var apt = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == AppointmentId);
        if (apt == null) return NotFound();

        apt.Update(
            Input.AppointmentDate, Input.Type, Input.Status,
            Input.DiagnosisCode, Input.Notes);

        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }

    private async Task LoadDoctorsAsync()
    {
        var doctors = await _mediator.Send(new GetDoctorSelectListQuery());
        DoctorsList = new SelectList(doctors, "Id", "FullName");
    }
}
