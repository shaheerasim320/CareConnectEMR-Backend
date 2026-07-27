using CareConnectEMR.Domain.Common;
using CareConnectEMR.Domain.Enitites;
using CareConnectEMR.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace CareConnectEMR.Infrastructure.Persistence
{
    public class AppDbContext:IdentityDbContext<ApplicationUser>
    {   
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private string? _auditReason;
        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options) { 
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Patient> Patients { get; set; }

        public DbSet<Appointment> Appointments { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        public void SetAuditReason(string reason) => _auditReason = reason;
        public void ClearAuditReason() => _auditReason = null;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.HasSequence<int>("PatientNumbers")
                .StartsAt(1000)
                .IncrementsBy(1);

            builder.Entity<ApplicationUser>(entity =>
            {

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
                .HasMaxLength(20)
                .HasDefaultValueSql("FORMAT(NEXT VALUE FOR PatientNumbers, 'MRN-0000')")
                .ValueGeneratedOnAdd()
                .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

                entity.Property(p => p.MRN).Metadata.SetBeforeSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
                entity.Property(p => p.MRN).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

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

                entity.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(PatientStatus.Active);

                entity.HasIndex(p => p.Status)
                .HasFilter("[Status] = 'Active'");

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

            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);

                entity.Property(rt => rt.UserId)
                .IsRequired();

                entity.Property(rt => rt.TokenHash)
                .IsRequired()
                .HasMaxLength(500);

                entity.Property(rt => rt.ExpiresAt)
                .IsRequired();

                entity.Property(rt => rt.DeviceInfo)
                .HasMaxLength(200);

                entity.Property(rt => rt.IpAddress)
                .HasMaxLength(100);

                entity.HasIndex(rt => rt.UserId)
                .HasDatabaseName("IDX_RefreshToken_UserId");

                entity.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(log => log.Id);
                entity.Property(log => log.EntityName).IsRequired().HasMaxLength(128);
                entity.Property(log => log.EntityId).IsRequired().HasMaxLength(128);
                entity.Property(log => log.Action).IsRequired().HasMaxLength(32);
                entity.Property(log => log.ChangedProperties).IsRequired().HasMaxLength(2000);
                entity.Property(log => log.Reason).HasMaxLength(500);
                entity.Property(log => log.UserId).HasMaxLength(450);
                entity.Property(log => log.RequestPath).HasMaxLength(500);
                entity.Property(log => log.IpAddress).HasMaxLength(100);
                entity.HasIndex(log => new { log.EntityName, log.EntityId, log.OccurredAt })
                    .HasDatabaseName("IDX_AuditLog_Entity_OccurredAt");
            });
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            var userId = httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "SYSTEM";
            var now = DateTime.UtcNow;
            var entries = ChangeTracker.Entries().Where(entry => entry.Entity is not AuditLog && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();

            foreach (var entry in entries.Where(entry => entry.Entity is IAuditable))
            {
                var auditable = (IAuditable)entry.Entity;
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = now;
                    auditable.CreatedBy = userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.UpdatedAt = now;
                    auditable.UpdatedBy = userId;
                    entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                    entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
                }
            }

            var excludedProperties = new HashSet<string> { "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "TokenHash" };
            var auditLogs = new List<AuditLog>();
            foreach (var entry in entries)
            {
                var changed = entry.Properties.Where(property => !excludedProperties.Contains(property.Metadata.Name) && (entry.State != EntityState.Modified || property.IsModified)).ToList();
                if (changed.Count == 0) continue;

                var key = entry.Metadata.FindPrimaryKey();
                var entityId = key is null ? string.Empty : string.Join("|", key.Properties.Select(property => entry.Property(property.Name).CurrentValue ?? entry.Property(property.Name).OriginalValue));
                var oldValues = entry.State == EntityState.Added ? null : changed.ToDictionary(property => property.Metadata.Name, property => property.OriginalValue);
                var newValues = entry.State == EntityState.Deleted ? null : changed.ToDictionary(property => property.Metadata.Name, property => property.CurrentValue);

                auditLogs.Add(new AuditLog
                {
                    EntityName = entry.Metadata.ClrType.Name,
                    EntityId = entityId,
                    Action = entry.State.ToString(),
                    ChangedProperties = string.Join(",", changed.Select(property => property.Metadata.Name)),
                    OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
                    NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues),
                    Reason = _auditReason,
                    UserId = userId,
                    OccurredAt = now,
                    RequestPath = httpContext?.Request.Path,
                    IpAddress = httpContext?.Connection.RemoteIpAddress?.MapToIPv4().ToString()
                });
            }
            AuditLogs.AddRange(auditLogs);
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
