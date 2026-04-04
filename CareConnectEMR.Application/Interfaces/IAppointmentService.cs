using CareConnectEMR.Application.Common;
using CareConnectEMR.Application.DTOs.Appointment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.Interfaces
{
    public interface IAppointmentService
    {
        Task<Result<PagedResult<AppointmentListResponse>>> GetAppointmentsAsync(AppointmentQueryParameters parameters, CancellationToken ct);

        Task<Result<AppointmentResponse>> GetAppointmentByIdAsync( Guid id, CancellationToken ct);

        Task<Result<AppointmentResponse>> CreateAppointmentAsync( CreateAppointmentRequest request, CancellationToken ct);

        Task<Result<AppointmentResponse>> UpdateAppointmentAsync( Guid id, UpdateAppointmentRequest request, CancellationToken ct);

        Task<Result<AppointmentResponse>> UpdateStatusAsync( Guid id, UpdateStatusRequest request, CancellationToken ct);

        Task<Result<string>> CancelAppointmentAsync( Guid id, CancelAppointmentRequest request, CancellationToken ct);
    }
}
