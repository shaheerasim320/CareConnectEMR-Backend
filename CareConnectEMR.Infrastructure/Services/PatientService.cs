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

        public async Task<Result<PagedResult<PatientListResponse>>> GetPatientsAsync(PatientQueryParameters parameters, CancellationToken ct)
        {
            if (parameters.Page < 1) parameters.Page = 1;

            var query = _context.Patients.AsNoTracking();

            if (parameters.IsDeleted.HasValue) query = query.Where(p => p.IsDeleted == parameters.IsDeleted.Value);

            if (!string.IsNullOrEmpty(parameters.Search))
            {
                var search = parameters.Search.ToLower().Trim();
                query = query.Where(p => p.FirstName.ToLower().Contains(search) || p.LastName.ToLower().Contains(search) || p.MRN.ToLower().Contains(search) || p.PhoneNumber.Contains(search) || (p.Email != null && p.Email.ToLower().Contains(search)));
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

        public async Task<Result<PatientResponse>> GetPatientByIdAsync(Guid id, CancellationToken ct)
        {
            var patient = await _context.Patients
            .AsNoTracking()
    .       FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

            if (patient is null)
                return Result<PatientResponse>.NotFound($"Patient with ID {id} not found.");

            return Result<PatientResponse>.Ok(PatientMapper.ToResponse(patient));
        }

        public async Task<Result<PatientResponse>> CreatePatientAsync(CreatePatientRequest request, CancellationToken ct)
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

        public async Task<Result<PatientResponse>> UpdatePatientAsync(Guid id, UpdatePatientRequest request, CancellationToken ct)
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
            if (patient == null)
            {
                return Result<PatientResponse>.NotFound($"Patient with ID {id} not found.");
            }
            patient.FirstName = request.FirstName.Trim();
            patient.LastName = request.LastName.Trim();
            patient.PhoneNumber = request.PhoneNumber;
            patient.DateOfBirth = request.DateOfBirth;
            patient.Gender = request.Gender;
            patient.Email = request.Email;
            patient.Address = request.Address;
            patient.BloodType = request.BloodType;
            patient.Allergies = request.Allergies;
            patient.EmergencyContactName = request.EmergencyContactName;
            patient.EmergencyContactNumber = request.EmergencyContactNumber;

            await _context.SaveChangesAsync(ct);
            return Result<PatientResponse>.Ok(PatientMapper.ToResponse(patient));
        }

        public async Task<Result<string>> DeletePatientAsync(Guid id, CancellationToken ct)
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
