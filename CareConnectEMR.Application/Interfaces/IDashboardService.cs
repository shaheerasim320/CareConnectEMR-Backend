using CareConnectEMR.Application.Common;
using CareConnectEMR.Application.DTOs.Dashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<Result<AdminDashboardResponse>> GetAdminDashboardAsync(CancellationToken ct);

        Task<Result<DoctorDashboardResponse>> GetDoctorDashboardAsync(string doctorId, CancellationToken ct);

        Task<Result<ReceptionistDashboardResponse>> GetReceptionistDashboardAsync(CancellationToken ct);
    }
}
