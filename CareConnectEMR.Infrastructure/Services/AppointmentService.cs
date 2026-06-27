using CareConnectEMR.Application.Common;
using CareConnectEMR.Application.DTOs.Appointment;
using CareConnectEMR.Application.Features.Appointment;
using CareConnectEMR.Application.Interfaces;
using CareConnectEMR.Domain.Enitites;
using CareConnectEMR.Domain.Enums;
using CareConnectEMR.Infrastructure.Persistence;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Infrastructure.Services
{
    public class AppointmentService:IAppointmentService
    {
        private readonly AppDbContext _context;
        public AppointmentService(AppDbContext context) => _context = context;

        private SqlConnection CreateConnection() => new(_context.Database.GetDbConnection().ConnectionString);

        public async Task<Result<PagedResult<AppointmentListResponse>>> GetAppointmentsAsync(AppointmentQueryParameters parameters, CancellationToken ct)
        {
            if (parameters.Page < 1) parameters.Page = 1;

            var query = _context.Appointments
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(parameters.DoctorId))
                query = query.Where(a => a.DoctorId == parameters.DoctorId);

            if (parameters.PatientId.HasValue)
                query = query.Where(a => a.PatientId == parameters.PatientId.Value);

            if (!string.IsNullOrEmpty(parameters.Status))
                query = query.Where(a => a.Status == parameters.Status);

            if (parameters.Date.HasValue)
            {
                var date = parameters.Date.Value.Date;
                query = query.Where(a => a.StartTime.Date == date);
            }

            if (parameters.From.HasValue)
                query = query.Where(a => a.StartTime >= parameters.From.Value);

            if (parameters.To.HasValue)
                query = query.Where(a => a.EndTime <= parameters.To.Value);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(a => a.StartTime)
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(a => new AppointmentListResponse
                {
                    Id = a.Id,
                    PatientName = a.Patient.FirstName + " " + a.Patient.LastName,
                    PatientMRN = a.Patient.MRN!,
                    DoctorName = a.Doctor.FirstName + " " + a.Doctor.LastName,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    DurationMinutes = (int)((double)(a.EndTime.Ticks - a.StartTime.Ticks)
                  / TimeSpan.TicksPerMinute),
                    Status = a.Status,
                    Reason = a.Reason,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync(ct);

            return Result<PagedResult<AppointmentListResponse>>.Ok(
                new PagedResult<AppointmentListResponse>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = parameters.Page,
                    PageSize = parameters.PageSize
                });
        }

        public async Task<Result<AppointmentResponse>> GetAppointmentByIdAsync(
            Guid id, CancellationToken ct)
        {
            var appointment = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (appointment is null)
                return Result<AppointmentResponse>.NotFound("Appointment not found.");

            return Result<AppointmentResponse>.Ok(AppointmentMapper.ToResponse(appointment));
        }
        public async Task<Result<AppointmentResponse>> CreateAppointmentAsync( CreateAppointmentRequest request, CancellationToken ct)
        {
            if (request.EndTime <= request.StartTime)
                return Result<AppointmentResponse>.Fail("EndTime must be after StartTime.");

            if (request.StartTime < DateTime.UtcNow)
                return Result<AppointmentResponse>.Fail("Cannot schedule an appointment in the past.");

            var duration = (request.EndTime - request.StartTime).TotalMinutes;
            if (duration < 10) 
                return Result<AppointmentResponse>.Fail("Appointment must be at least 10 minutes.");

            if (duration > 240) 
                return Result<AppointmentResponse>.Fail("Appointment cannot exceed 4 hours.");

            var patientExists = await _context.Patients.AnyAsync(p => p.Id == request.PatientId && p.Status == PatientStatus.Active, ct);
            if (!patientExists) 
                return Result<AppointmentResponse>.NotFound("Patient not found.");

            var doctorExists = await _context.Users.AnyAsync(u => u.Id == request.DoctorId, ct);
            if (!doctorExists) 
                return Result<AppointmentResponse>.NotFound("Doctor not found.");

            var hasConflict = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == request.DoctorId &&
                a.Status != AppointmentStatus.Cancelled &&
                a.Status != AppointmentStatus.NoShow &&
                a.StartTime < request.EndTime &&
                a.EndTime > request.StartTime, ct);

            if (hasConflict)
                return Result<AppointmentResponse>.Fail("Doctor already has an appointment during this time slot.");

            var appointment = new Appointment
            {
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Reason = request.Reason?.Trim(),
                Status = AppointmentStatus.Scheduled
            };

            await _context.Appointments.AddAsync(appointment, ct);
            await _context.SaveChangesAsync(ct);

            var created = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstAsync(a => a.Id == appointment.Id, ct);

            return Result<AppointmentResponse>.Created(AppointmentMapper.ToResponse(created));
        }

        public async Task<Result<AppointmentResponse>> UpdateAppointmentAsync( Guid id, UpdateAppointmentRequest request, CancellationToken ct)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (appointment is null)
                return Result<AppointmentResponse>.NotFound("Appointment not found.");

            if (appointment.Status != AppointmentStatus.Scheduled && appointment.Status != AppointmentStatus.Confirmed)
                return Result<AppointmentResponse>.Fail($"Cannot edit an appointment with status '{appointment.Status}'.");

            bool isChanged = false;

            var newStart = request.StartTime ?? appointment.StartTime;
            var newEnd = request.EndTime ?? appointment.EndTime;

            if (request.StartTime.HasValue || request.EndTime.HasValue)
            {
                if (newEnd <= newStart)
                    return Result<AppointmentResponse>.Fail("EndTime must be after StartTime.");

                if (newStart < DateTime.UtcNow)
                    return Result<AppointmentResponse>.Fail("Cannot reschedule to a past time.");

                var duration = (newEnd - newStart).TotalMinutes;
                if (duration < 10)
                    return Result<AppointmentResponse>.Fail("Appointment must be at least 10 minutes.");

                appointment.StartTime = newStart;
                appointment.EndTime = newEnd;
                isChanged = true;
            }

            if (request.Reason != null && appointment.Reason != request.Reason)
            {
                appointment.Reason = request.Reason.Trim();
                isChanged = true;
            }

            if (request.Notes != null && appointment.Notes != request.Notes)
            {
                appointment.Notes = request.Notes.Trim();
                isChanged = true;
            }

            if (!isChanged)
                return Result<AppointmentResponse>.Fail("No fields updated. Provide at least one changed field.");

            await _context.SaveChangesAsync(ct);
            return Result<AppointmentResponse>.Ok(AppointmentMapper.ToResponse(appointment));
        }

        public async Task<Result<AppointmentResponse>> UpdateStatusAsync(Guid id, UpdateStatusRequest request, CancellationToken ct)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (appointment is null)
                return Result<AppointmentResponse>.NotFound("Appointment not found.");

            if (!AppointmentStatus.All.Contains(request.Status))
                return Result<AppointmentResponse>.Fail($"Invalid status '{request.Status}'. " + $"Valid values: {string.Join(", ", AppointmentStatus.All)}");

            if (!AppointmentStatus.CanTransitionTo(appointment.Status, request.Status))
                return Result<AppointmentResponse>.Fail($"Cannot transition from '{appointment.Status}' to '{request.Status}'.");

            if (request.Status == AppointmentStatus.Cancelled)
            {
                if (string.IsNullOrWhiteSpace(request.CancellationReason))
                    return Result<AppointmentResponse>.Fail("CancellationReason is required when cancelling.");

                appointment.CancellationReason = request.CancellationReason.Trim();
            }

            appointment.Status = request.Status;

            if (request.Status == AppointmentStatus.Completed && request.RequiresFollowUp == true)
            {
                appointment.RequiresFollowUp = true;
                appointment.FollowUpDate = request.FollowUpDate;
            }

            await _context.SaveChangesAsync(ct);

            return Result<AppointmentResponse>.Ok(AppointmentMapper.ToResponse(appointment));
        }

        public async Task<Result<string>> CancelAppointmentAsync(Guid id, CancelAppointmentRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return Result<string>.Fail("Cancellation reason is required.");

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (appointment is null)
                return Result<string>.NotFound("Appointment not found.");

            if (appointment.Status == AppointmentStatus.Completed)
                return Result<string>.Fail("Cannot cancel a completed appointment.");

            if (appointment.Status == AppointmentStatus.Cancelled)
                return Result<string>.Fail("Appointment is already cancelled.");

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancellationReason = request.Reason.Trim();

            await _context.SaveChangesAsync(ct);
            return Result<string>.Ok("Appointment cancelled successfully.");
        }

        public async Task<Result<AppointmentStatsResponse>> GetAppointmentStatsAsync(string role, string currentUserId, CancellationToken ct = default)
        {
            using var connection = CreateConnection();
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            if (role == "Doctor")
            {
                const string sql = @"
                        SELECT COUNT(*) FROM Appointments a WHERE a.StartTime>=@Today AND a.StartTime<@Tomorrow AND a.DoctorId=@DoctorId
                        SELECT COUNT(*) FROM Appointments a WHERE a.StartTime>=@Today AND a.StartTime<@Tomorrow AND a.DoctorId=@DoctorId AND a.Status='Completed'
                        SELECT COUNT(*) FROM Appointments a WHERE a.StartTime>=@Today AND a.StartTime<@Tomorrow AND a.DoctorId=@DoctorId AND a.Status IN ('Scheduled','Confirmed')";

                using var multi = await connection.QueryMultipleAsync(sql, new { Today = today, Tomorrow = tomorrow, DoctorId = currentUserId });

                return Result<AppointmentStatsResponse>.Ok(new AppointmentStatsResponse
                {
                    AppointmentsToday = await multi.ReadSingleAsync<int>(),
                    CompletedToday = await multi.ReadSingleAsync<int>(),
                    RemainingToday = await multi.ReadSingleAsync<int>()
                });
            }

            const string adminSql = @"
                    SELECT COUNT(*) FROM Appointments a WHERE a.StartTime>=@Today AND a.StartTime<@Tomorrow
                    SELECT COUNT(*) FROM Appointments a WHERE a.StartTime>=@Today AND a.StartTime<@Tomorrow AND a.Status='Completed'
                    SELECT COUNT(*) FROM Appointments a WHERE a.StartTime>=@Today AND a.StartTime<@Tomorrow AND a.Status IN ('Cancelled','NoShow')";

            using var adminMulti = await connection.QueryMultipleAsync(adminSql, new { Today = today, Tomorrow = tomorrow });

            return Result<AppointmentStatsResponse>.Ok(new AppointmentStatsResponse
            {
                TotalToday = await adminMulti.ReadSingleAsync<int>(),
                CompletedToday = await adminMulti.ReadSingleAsync<int>(),
                CancelledOrNoShowToday = await adminMulti.ReadSingleAsync<int>()
            });
        }
    }
}
