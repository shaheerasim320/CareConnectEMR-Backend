using CareConnectEMR.Domain.Enitites;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CareConnectEMR.Infrastructure.Persistence.Seed
{
    public static class DataSeeder
    {
        public static async Task SeedDataAsync(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            if (await context.Patients.AnyAsync()) return;

            var doctors = await userManager.GetUsersInRoleAsync("Doctor");
            if (!doctors.Any()) return;

            var patients = await SeedPatientsAsync(context);
            await SeedAppointmentsAsync(context, patients, doctors.ToList());
        }

        private static async Task<List<Patient>> SeedPatientsAsync(AppDbContext context)
        {
            var patientList = new List<Patient>
            {
                new() { FirstName = "Zainab", LastName = "Abbas", DateOfBirth = new DateOnly(1990, 5, 12), Gender = "Female", PhoneNumber = "0300-5551122", BloodType = "A+", CreatedBy = "SYSTEM" },
                new() { FirstName = "Omar", LastName = "Farooq", DateOfBirth = new DateOnly(1985, 8, 24), Gender = "Male", PhoneNumber = "0321-4442233", BloodType = "O-", CreatedBy = "SYSTEM" },
                new() { FirstName = "Fatima", LastName = "Ibrahim", DateOfBirth = new DateOnly(1998, 12, 02), Gender = "Female", PhoneNumber = "0333-1119988", BloodType = "B+", CreatedBy = "SYSTEM" },
                new() { FirstName = "Bilal", LastName = "Siddiqui", DateOfBirth = new DateOnly(1975, 3, 15), Gender = "Male", PhoneNumber = "0345-8887766", BloodType = "AB+", CreatedBy = "SYSTEM" },
                new() { FirstName = "Ayesha", LastName = "Malik", DateOfBirth = new DateOnly(2002, 1, 30), Gender = "Female", PhoneNumber = "0301-2223344", BloodType = "A-", CreatedBy = "SYSTEM" }
            };

            string[] firstNames = { "Hamza", "Sara", "Mustafa", "Noor", "Hassan", "Anum", "Usman", "Zoya", "Ali", "Maham" };
            string[] lastNames = { "Ahmed", "Khan", "Sheikh", "Lodhi", "Raza", "Gillani", "Javed" };
            var rand = new Random();

            for (int i = 0; i < 25; i++)
            {
                var randomDob = DateTime.UtcNow.AddYears(-rand.Next(18, 70)).AddDays(rand.Next(0, 365));

                patientList.Add(new Patient
                {
                    FirstName = firstNames[rand.Next(firstNames.Length)],
                    LastName = lastNames[rand.Next(lastNames.Length)],
                    DateOfBirth = DateOnly.FromDateTime(randomDob),
                    Gender = rand.Next(2) == 0 ? "Male" : "Female",
                    PhoneNumber = $"03{rand.Next(0, 5)}{rand.Next(0, 9)}-{rand.Next(1000000, 9999999)}",
                    BloodType = "O+",
                    CreatedBy = "SYSTEM",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.Patients.AddRangeAsync(patientList);
            await context.SaveChangesAsync();
            return patientList;
        }

        private static async Task SeedAppointmentsAsync(AppDbContext context, List<Patient> patients, List<ApplicationUser> doctors)
        {
            var rand = new Random();
            var appointments = new List<Appointment>();
            string[] reasons = { "Monthly Checkup", "Flu Symptoms", "Chronic Back Pain", "Diabetes Consultation", "Post-Op Review", "Migraine", "Vaccination" };
            string[] statuses = AppointmentStatus.All;

            for (int i = 0; i < 50; i++)
            {
                var patient = patients[rand.Next(patients.Count)];
                var doctor = doctors[rand.Next(doctors.Count)];

                var startTime = DateTime.UtcNow.Date.AddDays(rand.Next(-3, 4)).AddHours(rand.Next(9, 17));

                appointments.Add(new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    StartTime = startTime,
                    EndTime = startTime.AddMinutes(30),
                    Status = statuses[rand.Next(statuses.Length)],
                    Reason = reasons[rand.Next(reasons.Length)],
                    Notes = "Seeded via system for testing.",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "SYSTEM"
                });
            }

            await context.Appointments.AddRangeAsync(appointments);
            await context.SaveChangesAsync();
        }
    }
}