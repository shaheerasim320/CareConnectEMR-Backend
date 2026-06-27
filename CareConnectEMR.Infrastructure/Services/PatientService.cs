using CareConnectEMR.Application.Common;
using CareConnectEMR.Application.DTOs.Patient;
using CareConnectEMR.Application.Features.Patient;
using CareConnectEMR.Application.Interfaces;
using CareConnectEMR.Domain.Enitites;
using CareConnectEMR.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CareConnectEMR.Domain.Enums;

namespace CareConnectEMR.Infrastructure.Services
{
    public class PatientService : IPatientService
    {
        private readonly AppDbContext _context;
        public PatientService(AppDbContext context)
        {
            _context = context;
        }

        private SqlConnection CreateConnection() => new(_context.Database.GetDbConnection().ConnectionString); 

        public async Task<Result<PagedResult<PatientListResponse>>> GetPatientsAsync(PatientQueryParameters parameters, string role, string currentUserId, CancellationToken ct = default)
        {
            if (parameters.Page < 1) parameters.Page = 1;

            var query = _context.Patients.AsNoTracking();

            if (role == "Admin")
            {
                query = parameters.IncludeAll ? query : query.Where(p => p.Status == (parameters.Status ?? PatientStatus.Active));
            }
            else
            {
                query = query.Where(p => p.Status == PatientStatus.Active);
            }


            if (role == "Doctor")
                query = query.Where(p => _context.Appointments.Any(a => a.PatientId == p.Id && a.DoctorId == currentUserId));

            if (!string.IsNullOrEmpty(parameters.Search))
            {
                var search = parameters.Search.ToLower().Trim();
                query = query.Where(p => p.FirstName.ToLower().Contains(search) || p.LastName.ToLower().Contains(search) || (p.MRN != null && p.MRN.ToLower().Contains(search)) || p.PhoneNumber.Contains(search) || (p.Email != null && p.Email.ToLower().Contains(search)));
            }

            var totalCount = await query.CountAsync(ct);


            var entities = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(ct);

            var items = entities.Select(PatientMapper.ToListItem).ToList();

            var pagedData = new PagedResult<PatientListResponse>
            {
                Items = items,
                TotalCount = totalCount,
                Page = parameters.Page,
                PageSize = parameters.PageSize
            };

            return Result<PagedResult<PatientListResponse>>.Ok(pagedData);
        }

        public async Task<Result<PatientResponse>> GetPatientByIdAsync(Guid id, string role, CancellationToken ct = default)
        {
            var query = _context.Patients.AsNoTracking();

            query = role == "Admin" ? query : query.Where(p => p.Status == PatientStatus.Active);

            var patient = await query.FirstOrDefaultAsync(p => p.Id == id, ct);

            if (patient is null)
                return Result<PatientResponse>.NotFound($"Patient with ID {id} not found.");

            return Result<PatientResponse>.Ok(PatientMapper.ToResponse(patient));
        }

        public async Task<Result<PatientResponse>> CreatePatientAsync(CreatePatientRequest request, CancellationToken ct = default)
        {
            var nextSequenceValue = await _context.Database
                .SqlQueryRaw<int>("SELECT NEXT VALUE FOR dbo.PatientNumbers")
                .ToListAsync(ct);

            int sequenceId = nextSequenceValue.First();

            string generatedMRN = $"MRN-{DateTime.UtcNow.Year}-{sequenceId:D6}";

            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                MRN = generatedMRN,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                Address = request.Address,
                BloodType = request.BloodType,
                Allergies = request.Allergies,
                EmergencyContactName = request.EmergencyContactName,
                EmergencyContactNumber = request.EmergencyContactNumber,
            };

            await _context.Patients.AddAsync(patient, ct);
            await _context.SaveChangesAsync(ct);

            return Result<PatientResponse>.Created(PatientMapper.ToResponse(patient));
        }

        public async Task<Result<PatientResponse>> UpdatePatientAsync(Guid id, UpdatePatientRequest request, CancellationToken ct = default)
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == id && p.Status == PatientStatus.Active, ct);
            if (patient == null) return Result<PatientResponse>.NotFound($"Patient with ID {id} not found.");
            
            bool isChanged = false;

            if (request.FirstName != null && patient.FirstName != request.FirstName ) { patient.FirstName = request.FirstName.Trim(); isChanged = true; }
            if (request.LastName != null && patient.LastName != request.LastName) { patient.LastName = request.LastName.Trim(); isChanged = true; }
            if (request.PhoneNumber != null && patient.PhoneNumber != request.PhoneNumber) { patient.PhoneNumber = request.PhoneNumber; isChanged = true; }
            if (request.DateOfBirth.HasValue && patient.DateOfBirth != request.DateOfBirth.Value) { patient.DateOfBirth = request.DateOfBirth.Value; isChanged = true; }
            if (request.Gender != null && patient.Gender != request.Gender) { patient.Gender = request.Gender;isChanged = true; }
            if (request.Email != null && patient.Email != request.Email) { patient.Email = request.Email; isChanged = true; }
            if (request.Address != null && patient.Address != request.Address) { patient.Address = request.Address; isChanged = true; }
            if (request.BloodType != null && patient.BloodType != request.BloodType) { patient.BloodType = request.BloodType; isChanged = true; }
            if (request.Allergies != null && patient.Allergies != request.Allergies) { patient.Allergies = request.Allergies; isChanged = true; }
            if (request.EmergencyContactName != null && patient.EmergencyContactName != request.EmergencyContactName) { patient.EmergencyContactName = request.EmergencyContactName; isChanged = true; }
            if (request.EmergencyContactNumber != null && patient.EmergencyContactNumber != request.EmergencyContactNumber) { patient.EmergencyContactNumber = request.EmergencyContactNumber; isChanged = true; }

            if(!isChanged) return Result<PatientResponse>.Fail("No fields were updated. Please provide at least one field to update.");

            await _context.SaveChangesAsync(ct);
            return Result<PatientResponse>.Ok(PatientMapper.ToResponse(patient));
        }

        public async Task<Result<PatientStatResponse>> GetPatientStatsAsync(string role, string currentUserId, CancellationToken ct=default)
        {
            using var connection = CreateConnection();
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            if (role == "Doctor")
            {
                const string sql = @"
                        SELECT COUNT(DISTINCT a.PatientId) FROM Appointments a 
                        WHERE a.StartTime>=@Today AND a.StartTime<@Tomorrow AND a.DoctorId=@DoctorId AND a.Status = 'CheckedIn';
                        
                        SELECT COUNT(DISTINCT PatientId) FROM Appointments 
                        WHERE StartTime>=@Today AND StartTime<@Tomorrow AND Status = 'Completed' AND DoctorId=@DoctorId

                        SELECT COUNT(DISTINCT a.PatientId)
                        FROM Appointments a
                        INNER JOIN Patients p ON p.Id = a.PatientId
                        WHERE a.DoctorId=@DoctorId AND a.RequiresFollowUp=1 AND a.FollowUpDate IS NOT NULL
                          AND CAST(a.FollowUpDate AS DATE)<=@TODAY
                          AND p.Status = 'Active'
                          AND NOT EXISTS(SELECT 1 FROM Appointments later WHERE later.PatientId=a.PatientId AND later.DoctorId=a.DoctorId AND later.StartTime>=a.FollowUpDate AND later.Status NOT IN ('Cancelled','NoShow'));";

                using var multi = await connection.QueryMultipleAsync(sql, new { Today = today, Tomorrow = tomorrow, DoctorId = currentUserId });

                return Result<PatientStatResponse>.Ok(new PatientStatResponse
                {
                    PatientsWaiting = await multi.ReadSingleAsync<int>(),
                    SeenToday = await multi.ReadSingleAsync<int>(),
                    FollowUpsDue = await multi.ReadSingleAsync<int>()
                });
            }

            const string adminSql = @"
                        SELECT COUNT(*) FROM Patients WHERE Status = 'Active';
                        SELECT COUNT(*) FROM Patients WHERE CreatedAt>=@TODAY AND CreatedAt<@TOMORROW AND Status = 'Active';
                        SELECT COUNT(*) FROM Patients WHERE (Address IS NULL OR LTRIM(RTRIM(Address)) = '' OR BloodType IS NULL OR LTRIM(RTRIM(BloodType)) = '' OR EmergencyContactName IS NULL OR LTRIM(RTRIM(EmergencyContactName)) = '' OR EmergencyContactNumber IS NULL OR LTRIM(RTRIM(EmergencyContactNumber)) = '') AND Status = 'Active';";

            using var adminMulti = await connection.QueryMultipleAsync(adminSql, new { Today = today, Tomorrow = tomorrow });

            return Result<PatientStatResponse>.Ok(new PatientStatResponse
            {
                TotalPatients = await adminMulti.ReadSingleAsync<int>(),
                RegisteredToday = await adminMulti.ReadSingleAsync<int>(),
                IncompleteRecords = await adminMulti.ReadSingleAsync<int>()
            });
        }

        public async Task<Result<string>> UpdatePatientStatusAsync(Guid id, PatientStatus status, CancellationToken ct = default)
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (patient == null)
                return Result<string>.NotFound($"Patient with ID {id} not found.");

            if (patient.Status == status)
                return Result<string>.Fail($"Patient is already {status}.");

            patient.Status = status;
            await _context.SaveChangesAsync(ct);
            return Result<string>.Ok($"Patient with ID {id} has been set to {status}.");
        }
    }
}
