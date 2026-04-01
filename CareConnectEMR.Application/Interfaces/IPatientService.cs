using CareConnectEMR.Application.Common;
using CareConnectEMR.Application.DTOs.Patient;
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
        Task<Result<PatientResponse>> GetPatientByIdAsync(Guid patientId, CancellationToken ct);
        Task<Result<PagedResult<PatientListResponse>>> GetPatientsAsync(PatientQueryParameters paramters, CancellationToken ct);
        Task<Result<PatientResponse>> UpdatePatientAsync(Guid patientId, UpdatePatientRequest request, CancellationToken ct);
        Task<Result<string>> DeletePatientAsync(Guid patientId, CancellationToken ct);
    }
}
