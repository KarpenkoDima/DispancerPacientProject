using DispancerPacient.Application.Interfaces;
using DispancerPacient.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DispancerPacient.Web.Pages.Doctors;

public class EditModel : PageModel
{
    private readonly IAppDbContext _db;

    public EditModel(IAppDbContext db) => _db = db;

    [BindProperty]
    public DoctorFormModel Input { get; set; } = null!;

    [BindProperty]
    public int DoctorId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == id);
        if (doctor == null) return NotFound();

        DoctorId = id;
        Input = new DoctorFormModel
        {
            LastName = doctor.LastName,
            FirstName = doctor.FirstName,
            MiddleName = doctor.MiddleName,
            Specialization = doctor.Specialization,
            LicenseNumber = doctor.LicenseNumber,
            Phone = doctor.Phone,
            Email = doctor.Email,
            IsActive = doctor.IsActive
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == DoctorId);
        if (doctor == null) return NotFound();

        doctor.Update(
            Input.LastName, Input.FirstName, Input.MiddleName,
            Input.Specialization, Input.LicenseNumber,
            Input.Phone, Input.Email, Input.IsActive);

        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
