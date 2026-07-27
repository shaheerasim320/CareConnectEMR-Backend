using CareConnectEMR.Application.Common;
using CareConnectEMR.Application.DTOs.Patient;
using CareConnectEMR.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.Interfaces
{
    public interface IPatientService
    {
        Task<Result<PatientResponse>> CreatePatientAsync(CreatePatientRequest request, CancellationToken ct);
        Task<Result<PatientResponse>> GetPatientByIdAsync(Guid patientId, string role, string currentUserId, CancellationToken ct);
        Task<Result<PagedResult<PatientListResponse>>> GetPatientsAsync(PatientQueryParameters paramters, string role, string currentUserId, PatientStatus? status, bool includeAll, CancellationToken ct);
        Task<Result<PatientResponse>> UpdateContactAsync(Guid patientId, UpdatePatientContactRequest request, CancellationToken ct);
        Task<Result<PatientResponse>> UpdateIdentityAsync(Guid patientId, UpdatePatientIdentityRequest request, CancellationToken ct);
        Task<Result<PatientResponse>> UpdateClinicalAsync(Guid patientId, UpdatePatientClinicalRequest request, CancellationToken ct);
        Task<Result<PatientStatResponse>> GetPatientStatsAsync(string role, string currentUserId, CancellationToken ct);
        Task<Result<string>> UpdatePatientStatusAsync(Guid patientId, PatientStatus status, CancellationToken ct);
    }
}
