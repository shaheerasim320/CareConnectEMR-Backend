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
        Task<Result<PagedResult<AppointmentListResponse>>> GetAppointmentsAsync(AppointmentQueryParameters parameters, string role, string currentUserId, CancellationToken ct);
        Task<Result<AppointmentResponse>> GetAppointmentByIdAsync(Guid id, string role, string currentUserId, CancellationToken ct);
        Task<Result<AppointmentResponse>> CreateAppointmentAsync( CreateAppointmentRequest request, CancellationToken ct);
        Task<Result<AppointmentResponse>> UpdateAppointmentAsync( Guid id, UpdateAppointmentRequest request, CancellationToken ct);
        Task<Result<AppointmentResponse>> UpdateClinicalNotesAsync(Guid id, UpdateAppointmentNotesRequest request, string currentUserId, CancellationToken ct);
        Task<Result<AppointmentResponse>> UpdateStatusAsync(Guid id, UpdateStatusRequest request, string role, string currentUserId, CancellationToken ct);
        Task<Result<string>> CancelAppointmentAsync( Guid id, CancelAppointmentRequest request, CancellationToken ct);
        Task<Result<AppointmentStatsResponse>> GetAppointmentStatsAsync(string role, string currentUserId, CancellationToken ct);
    }
}
