using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using CareConnectEMR.Domain.Common;
using CareConnectEMR.Domain.Enitites;

namespace CareConnectEMR.Infrastructure.Persistence
{
    public class AppDbContext:IdentityDbContext<ApplicationUser>
    {   
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options) { 
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Patient> Patients { get; set; }

        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.HasSequence<int>("PatientNumbers")
                .StartsAt(1000)
                .IncrementsBy(1);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.HasIndex(u => u.RefreshToken)
                .HasDatabaseName("IDX_RefreshToken");

                entity.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(50);

                entity.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(50);

                entity.HasIndex(u => new {u.FirstName,u.LastName})
                .HasDatabaseName("IDX_User_FullName");

                entity.HasIndex(u => u.IsActive)
                .HasFilter("[IsActive] = 1");

                entity.Ignore(u => u.FullName);

            });

            builder.Entity<Patient>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.MRN)
                .IsRequired()
                .HasMaxLength(20);

                entity.HasIndex(p=>p.MRN)
                .IsUnique()
                .HasDatabaseName("IDX_MRN");

                entity.Property(p=>p.FirstName)
                .IsRequired()
                .HasMaxLength(50);

                entity.Property(p => p.LastName)
                .IsRequired()
                .HasMaxLength(50);

                entity.HasIndex(p => new { p.FirstName, p.LastName })
                .HasDatabaseName("IDX_Patient_Name");

                entity.Property(p => p.DateOfBirth)
                .IsRequired();

                entity.HasIndex(p=>p.DateOfBirth)
                .HasDatabaseName("IDX_Patient_DOB");

                entity.Property(p => p.Gender)
                .IsRequired()
                .HasMaxLength(10);

                entity.Property(p => p.PhoneNumber)
                .IsRequired()
                .HasMaxLength(15);

                entity.HasIndex(p=>p.PhoneNumber)
                .HasDatabaseName("IDX_Patient_Phone");

                entity.Property(p => p.Email)
                .HasMaxLength(100);

                entity.Property(p=> p.Address)
                .HasMaxLength(200);

                entity.Property(p => p.BloodType)
                .HasMaxLength(5);

                entity.HasIndex(p => p.IsDeleted)
                .HasFilter("[IsDeleted] = 0");

                entity.Ignore(p=>p.FullName);
                entity.Ignore(p => p.Age);
            });

            builder.Entity<Appointment>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.PatientId)
                .IsRequired();

                entity.Property(a => a.DoctorId)
                .IsRequired();

                entity.Property(a => a.StartTime)
                .IsRequired();

                entity.Property(a => a.EndTime)
                .IsRequired();

                entity.Property(a => a.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue(AppointmentStatus.Scheduled);

                entity.Property(a => a.Reason)
                .HasMaxLength(500);

                entity.Property(a => a.Notes)
                .HasMaxLength(2000);

                entity.Property(a => a.CancellationReason)
                .HasMaxLength(500);

                entity.Ignore(a => a.DurationMinutes);

                entity.HasIndex(a => new { a.DoctorId, a.StartTime, a.EndTime })
                .HasDatabaseName("IDX_Appointment_Doctor_TimeRange");

                entity.HasIndex(a => a.PatientId)
                .HasDatabaseName("IDX_Appointment_PatientId");

                entity.HasIndex(a=>a.Status)
                .HasDatabaseName("IDX_Appointment_Status");

                entity.HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
            });
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "SYSTEM";
            var entries = ChangeTracker.Entries<IAuditable>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = userId;  
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = userId;
                    entry.Property(x => x.CreatedAt).IsModified = false;
                    entry.Property(x => x.CreatedBy).IsModified = false;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
