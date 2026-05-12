using DispancerPacient.Application.Interfaces;
using DispancerPacient.Domain.Enums;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DispancerPacient.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IAppDbContext _db;

    public IndexModel(IAppDbContext db) => _db = db;

    public int PatientCount { get; set; }
    public int DoctorCount { get; set; }
    public int TodayAppointments { get; set; }

    public async Task OnGetAsync()
    {
        PatientCount = await _db.Patients.CountAsync(p => p.IsOnRecord);
        DoctorCount = await _db.Doctors.CountAsync(d => d.IsActive);
        TodayAppointments = await _db.Appointments.CountAsync(a =>
            a.AppointmentDate.Date == DateTime.Today &&
            a.Status == AppointmentStatus.Scheduled);
    }
}
