using DispancerPacient.Domain.Entities;
using DispancerPacient.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DispancerPacient.Web.Pages.Doctors;

public class CreateModel : PageModel
{
    private readonly IAppDbContext _db;

    public CreateModel(IAppDbContext db) => _db = db;

    [BindProperty]
    public DoctorFormModel Input { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var doctor = Doctor.Create(
            Input.LastName, Input.FirstName, Input.MiddleName,
            Input.Specialization, Input.LicenseNumber,
            Input.Phone, Input.Email);

        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}

public class DoctorFormModel
{
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Specialization { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
}
