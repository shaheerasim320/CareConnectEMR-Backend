using CareConnectEMR.Application.Common;
using CareConnectEMR.Application.DTOs.Patient;
using CareConnectEMR.Application.Features.Patient;
using CareConnectEMR.Application.Interfaces;
using CareConnectEMR.Domain.Enitites;
using CareConnectEMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Infrastructure.Services
{
    public class PatientService : IPatientService
    {
        private readonly AppDbContext _context;
        public PatientService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<PagedResult<PatientListResponse>>> GetPatientsAsync(PatientQueryParameters parameters, string role, string currentUserId, CancellationToken ct = default)
        {
            if (parameters.Page < 1) parameters.Page = 1;

            var query = _context.Patients.AsNoTracking();

            query = parameters.IsDeleted.HasValue ? query.Where(p => p.IsDeleted == parameters.IsDeleted.Value) : query.Where(p => !p.IsDeleted);

            if (role == "Doctor")
                query = query.Where(p => _context.Appointments.Any(a => a.PatientId == p.Id && a.DoctorId == currentUserId));

            if (!string.IsNullOrEmpty(parameters.Search))
            {
                var search = parameters.Search.ToLower().Trim();
                query = query.Where(p => p.FirstName.ToLower().Contains(search) || p.LastName.ToLower().Contains(search) || (p.MRN != null && p.MRN.ToLower().Contains(search)) || p.PhoneNumber.Contains(search) || (p.Email != null && p.Email.ToLower().Contains(search)));
            }

            var totalCount = await query.CountAsync(ct);

            
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(p => new PatientListResponse
                {
                    Id = p.Id,
                    MRN = p.MRN,
                    FullName = p.FirstName + " " + p.LastName,
                    Age = DateTime.UtcNow.Year - p.DateOfBirth.Year,
                    Gender = p.Gender,
                    PhoneNumber = p.PhoneNumber,
                    BloodType = p.BloodType,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync(ct);

            var pagedData = new PagedResult<PatientListResponse>
            {
                Items = items,
                TotalCount = totalCount,
                Page = parameters.Page,
                PageSize = parameters.PageSize
            };

            return Result<PagedResult<PatientListResponse>>.Ok(pagedData);
        }

        public async Task<Result<PatientResponse>> GetPatientByIdAsync(Guid id, CancellationToken ct = default)
        {
            var patient = await _context.Patients
            .AsNoTracking()
    .       FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

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
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
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

        public async Task<Result<string>> DeletePatientAsync(Guid id, CancellationToken ct = default)
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
            if (patient == null)
            {
                return Result<string>.NotFound($"Patient with ID {id} not found.");
            }
            patient.IsDeleted = true;
            await _context.SaveChangesAsync(ct);
            return Result<string>.Ok($"Patient with ID {id} has been deleted.");
        }
    }
}
