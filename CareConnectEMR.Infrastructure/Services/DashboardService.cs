using CareConnectEMR.Application.Common;
using CareConnectEMR.Application.DTOs.Dashboard;
using CareConnectEMR.Application.DTOs.Dashboard.Shared;
using CareConnectEMR.Application.Enums;
using CareConnectEMR.Application.Interfaces;
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
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context) => _context = context;

        private SqlConnection CreateConnection() => new(_context.Database.GetDbConnection().ConnectionString);

        private StatCard BuildPercentStat(int count, int previous, TrendComparison comparison, bool allowZeroPrevious = true)
        {
            decimal? trend = null;
            TrendDirection direction = TrendDirection.Neutral;
            if (previous > 0)
            {
                trend = Math.Round(((decimal)(count - previous) / previous) * 100, 1);
            }
            else if (previous == 0)
            {
                if (count > 0 && allowZeroPrevious)
                {
                    trend = 100.0m;
                }
                else if (count == 0 && allowZeroPrevious)
                {
                    trend = 0.0m;
                }                
            }

            if (trend.HasValue)
            {
                if (trend > 0) direction = TrendDirection.Up;
                else if (trend < 0) direction = TrendDirection.Down;
            }
            return new StatCard
            {
                Count = count,
                TrendValue = trend,
                TrendType = TrendType.Percent,
                TrendDirection = direction,
                TrendComparison = comparison
            };
        }

        private StatCard BuildNumberStat(int count, int previous, TrendComparison comparison)
        {
            var diff = count - previous;

            var direction = diff > 0 ? TrendDirection.Up : diff < 0 ? TrendDirection.Down : TrendDirection.Neutral;

            return new StatCard
            {
                Count = count,
                TrendValue = Math.Abs(diff),
                TrendType = TrendType.Number,
                TrendDirection = direction,
                TrendComparison = comparison
            };
        }

        private StatCard BuildSimpleStat(int count, TrendComparison comparison)
        {
            return new StatCard
            {
                Count = count,
                TrendComparison = comparison
            };
        }
        public async Task<Result<AdminDashboardResponse>> GetAdminDashboardAsync(CancellationToken ct)
        {
            var kpiSql = """
            DECLARE @Today          DATE = CAST(GETUTCDATE() AS DATE);
            DECLARE @Tomorrow       DATE = DATEADD(DAY, 1, @Today);
            DECLARE @Yesterday      DATE = DATEADD(DAY, -1, @Today);   
            DECLARE @MonthStart     DATE = DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1);
            DECLARE @LastMonthStart DATE = DATEFROMPARTS(YEAR(DATEADD(MONTH,-1,@Today)), MONTH(DATEADD(MONTH,-1,@Today)), 1);
            DECLARE @LastMonthEnd   DATE = @MonthStart;

            SELECT
            	--Total Patients this month vs last month
            	(SELECT COUNT(*) FROM Patients WHERE IsDeleted=0) AS TotalPatients_Count,

            	(SELECT COUNT(*) FROM Patients WHERE IsDeleted=0 AND CreatedAt<@LastMonthEnd AND CreatedAt>=@LastMonthStart) AS TotalPatients_PreviousMonthCount,

            	--Appointments today vs yesterday
            	(SELECT COUNT(*) FROM Appointments WHERE StartTime>=@Today AND StartTime<@Tomorrow ) AS AppointmentsToday_Count,

            	(SELECT COUNT(*) FROM Appointments WHERE StartTime>=@Yesterday AND StartTime<@Today) AS AppointmentsPreviousDay_Count,

            	--Completed today vs yesterday same slot
            	(SELECT COUNT(*) FROM Appointments WHERE StartTime>=@Today AND StartTime<@Tomorrow AND Status='Completed') AS CompletedToday_Count,

            	(SELECT COUNT(*) FROM Appointments WHERE StartTime>=@Yesterday AND StartTime<@Today AND Status='Completed') AS CompletedYesterday_Count,

            	-- Cancellation rate today
            	CASE
            		WHEN (SELECT COUNT(*) FROM Appointments WHERE StartTime>=@Today AND StartTime<@Tomorrow) = 0
            		THEN 0
            		ELSE (SELECT COUNT(*) FROM Appointments WHERE Status='Cancelled' AND StartTime>=@Today AND StartTime<@Tomorrow)*100
            			/(SELECT COUNT(*) FROM Appointments WHERE StartTime>=@Today AND StartTime<@Tomorrow)
            		END AS CancellationRate_Count,

                -- Cancellation rate for yesterday
            	CASE
            		WHEN (SELECT COUNT(*) FROM Appointments WHERE StartTime>=@Yesterday AND StartTime<@Today) = 0
            		THEN 0
            		ELSE (SELECT COUNT(*) FROM Appointments WHERE Status='Cancelled' AND StartTime>=@Yesterday AND StartTime<@Today)*100
            			/(SELECT COUNT(*) FROM Appointments WHERE StartTime>=@Yesterday AND StartTime<@Today)
            		END AS CancellationRate_YesterdayCount
            
            """;

            var breakdownSql = """
            DECLARE @Today    DATE = CAST(GETUTCDATE() AS DATE);
            DECLARE @Tomorrow DATE = DATEADD(DAY, 1, @Today);

            SELECT
                SUM(CASE WHEN Status = 'Scheduled'  THEN 1 ELSE 0 END) AS Scheduled,
                SUM(CASE WHEN Status = 'Confirmed'  THEN 1 ELSE 0 END) AS Confirmed,
                SUM(CASE WHEN Status = 'CheckedIn'  THEN 1 ELSE 0 END) AS CheckedIn,
                SUM(CASE WHEN Status = 'Completed'  THEN 1 ELSE 0 END) AS Completed,
                SUM(CASE WHEN Status = 'Cancelled'  THEN 1 ELSE 0 END) AS Cancelled,
                SUM(CASE WHEN Status = 'NoShow'     THEN 1 ELSE 0 END) AS NoShow
            FROM Appointments
            WHERE StartTime >= @Today AND StartTime < @Tomorrow
            """;

            var doctorsSql = """
            DECLARE @Today    DATE = CAST(GETUTCDATE() AS DATE);
            DECLARE @Tomorrow DATE = DATEADD(DAY, 1, @Today);

            SELECT TOP 5
                a.DoctorId,
                u.FirstName + ' ' + u.LastName                              AS DoctorName,
                COUNT(*)                                                     AS AppointmentCount,
                SUM(CASE WHEN a.Status = 'Completed' THEN 1 ELSE 0 END)     AS CompletedCount
            FROM Appointments a
            INNER JOIN AspNetUsers u ON a.DoctorId = u.Id
            WHERE a.StartTime >= @Today AND a.StartTime < @Tomorrow
            GROUP BY a.DoctorId, u.FirstName, u.LastName
            ORDER BY AppointmentCount DESC
            """;

            var recentSql = """
            SELECT TOP 5
                Id,
                FirstName + ' ' + LastName  AS FullName,
                MRN,
                Gender,
                CreatedAt                   AS RegisteredAt
            FROM Patients
            WHERE IsDeleted = 0
            ORDER BY CreatedAt DESC
            """;

            using var conn = CreateConnection();

            var kpiRaw = await conn.QuerySingleAsync(kpiSql);
            var breakdown = await conn.QuerySingleAsync<AppointmentBreakdown>(breakdownSql);
            var topDoctors = (await conn.QueryAsync<DoctorLoad>(doctorsSql)).ToList();
            var recentPats = (await conn.QueryAsync<RecentPatient>(recentSql)).ToList();

            var response = new AdminDashboardResponse
            {
                TotalPatients = BuildPercentStat((int)kpiRaw.TotalPatients_Count, (int)kpiRaw.TotalPatients_PreviousMonthCount,TrendComparison.Month,allowZeroPrevious: false),
                AppointmentsToday = BuildPercentStat((int)kpiRaw.AppointmentsToday_Count, (int)kpiRaw.AppointmentsPreviousDay_Count,TrendComparison.Yesterday),
                CompletedToday = BuildPercentStat((int)kpiRaw.CompletedToday_Count,(int)kpiRaw.CompletedYesterday_Count,TrendComparison.Yesterday),
                CancellationRate = BuildPercentStat((int)kpiRaw.CancellationRate_Count,(int)kpiRaw.CancellationRate_YesterdayCount,TrendComparison.Yesterday),
                BreakdownToday = breakdown,
                TopDoctorsToday = topDoctors,
                RecentRegistrations = recentPats
            };

            return Result<AdminDashboardResponse>.Ok(response);
        }

        public async Task<Result<DoctorDashboardResponse>> GetDoctorDashboardAsync(string doctorId, CancellationToken ct)
        {
            var kpiSql = """
            DECLARE @Today     DATE = CAST(GETUTCDATE() AS DATE);
            DECLARE @Tomorrow  DATE = DATEADD(DAY,  1, @Today);
            DECLARE @Yesterday DATE = DATEADD(DAY, -1, @Today);

            SELECT
                -- My appointments today vs yesterday
                (SELECT COUNT(*) FROM Appointments
                 WHERE DoctorId = @DoctorId
                   AND StartTime >= @Today AND StartTime < @Tomorrow)
                    AS MyAppointmentsToday_Count,

                (SELECT COUNT(*) FROM Appointments
                 WHERE DoctorId = @DoctorId
                   AND StartTime >= @Yesterday AND StartTime < @Today)
                    AS MyAppointmentsToday_PreviousCount,

                -- My completed today vs yesterday
                (SELECT COUNT(*) FROM Appointments
                 WHERE DoctorId = @DoctorId AND Status = 'Completed'
                   AND StartTime >= @Today AND StartTime < @Tomorrow)
                    AS MyCompletedToday_Count,

                (SELECT COUNT(*) FROM Appointments
                 WHERE DoctorId = @DoctorId AND Status = 'Completed'
                   AND StartTime >= @Yesterday AND StartTime < @Today)
                    AS MyCompletedToday_PreviousCount,

                -- Career total — all completed appointments ever
                (SELECT COUNT(*) FROM Appointments
                 WHERE DoctorId = @DoctorId AND Status = 'Completed')
                    AS TotalPatientsSeen
            """;

            var nextSql = """
            SELECT TOP 1
                a.Id,
                p.FirstName + ' ' + p.LastName  AS PatientName,
                p.MRN                            AS PatientMRN,
                a.StartTime,
                a.EndTime,
                a.Status,
                a.Reason
            FROM Appointments a
            INNER JOIN Patients p ON a.PatientId = p.Id
            WHERE a.DoctorId  = @DoctorId
              AND a.StartTime >= GETUTCDATE()
              AND a.Status    IN ('Scheduled','Confirmed','CheckedIn')
            ORDER BY a.StartTime ASC
            """;

            var scheduleSql = """
            DECLARE @Today    DATE = CAST(GETUTCDATE() AS DATE);
            DECLARE @Tomorrow DATE = DATEADD(DAY, 1, @Today);

            SELECT
                a.Id,
                p.FirstName + ' ' + p.LastName  AS PatientName,
                p.MRN                            AS PatientMRN,
                a.StartTime,
                a.EndTime,
                a.Status,
                a.Reason
            FROM Appointments a
            INNER JOIN Patients p ON a.PatientId = p.Id
            WHERE a.DoctorId  = @DoctorId
              AND a.StartTime >= @Today
              AND a.StartTime <  @Tomorrow
            ORDER BY a.StartTime ASC
            """;

            var param = new { DoctorId = doctorId };

            using var conn = CreateConnection();

            var kpiRaw = await conn.QuerySingleAsync(kpiSql, param);
            var next = await conn.QueryFirstOrDefaultAsync<NextAppointmentDto>(nextSql, param);
            var sched = (await conn.QueryAsync<TodayScheduleItem>(scheduleSql, param)).ToList();

            var response = new DoctorDashboardResponse
            {
                MyAppointmentsToday = BuildNumberStat((int)kpiRaw.MyAppointmentsToday_Count, (int)kpiRaw.MyAppointmentsToday_PreviousCount,TrendComparison.Yesterday),
                MyCompletedToday = new StatCard
                {
                    Count = (int)kpiRaw.MyCompletedToday_Count,
                    TrendValue = (int)kpiRaw.MyAppointmentsToday_Count - (int)kpiRaw.MyCompletedToday_Count,
                    TrendType = TrendType.Number,
                    TrendDirection = TrendDirection.Neutral,
                    TrendComparison = TrendComparison.Remaining
                },
                TotalPatientsSeen = BuildSimpleStat((int)kpiRaw.TotalPatientsSeen,TrendComparison.Career),
                NextAppointment = next,
                TodaySchedule = sched
            };

            return Result<DoctorDashboardResponse>.Ok(response);
        }

        public async Task<Result<ReceptionistDashboardResponse>> GetReceptionistDashboardAsync(
            CancellationToken ct)
        {
            var kpiSql = """
            DECLARE @Today     DATE = CAST(GETUTCDATE() AS DATE);
            DECLARE @Tomorrow  DATE = DATEADD(DAY,  1, @Today);
            DECLARE @Yesterday DATE = DATEADD(DAY, -1, @Today);

            SELECT
                -- Today's total vs yesterday
                (SELECT COUNT(*) FROM Appointments
                 WHERE StartTime >= @Today AND StartTime < @Tomorrow)
                    AS AppointmentsToday_Count,

                (SELECT COUNT(*) FROM Appointments
                 WHERE StartTime >= @Yesterday AND StartTime < @Today)
                    AS AppointmentsToday_PreviousCount,

                -- Live checked-in count — no trend, it changes minute to minute
                (SELECT COUNT(*) FROM Appointments
                 WHERE Status   =  'CheckedIn'
                   AND StartTime >= @Today AND StartTime < @Tomorrow)
                    AS CheckedInNow,

                -- New patients registered today vs yesterday
                (SELECT COUNT(*) FROM Patients
                 WHERE IsDeleted = 0
                   AND CAST(CreatedAt AS DATE) = @Today)
                    AS NewPatientsToday_Count,

                (SELECT COUNT(*) FROM Patients
                 WHERE IsDeleted = 0
                   AND CAST(CreatedAt AS DATE) = @Yesterday)
                    AS NewPatientsToday_PreviousCount
            """;

            var queueSql = """
            DECLARE @Today    DATE = CAST(GETUTCDATE() AS DATE);
            DECLARE @Tomorrow DATE = DATEADD(DAY, 1, @Today);

            SELECT
                a.Id,
                p.FirstName + ' ' + p.LastName      AS PatientName,
                p.MRN                                AS PatientMRN,
                u.FirstName + ' ' + u.LastName       AS DoctorName,
                a.StartTime,
                a.EndTime,
                a.Status,
                a.Reason
            FROM Appointments a
            INNER JOIN Patients p ON a.PatientId = p.Id
            INNER JOIN AspNetUsers u ON a.DoctorId  = u.Id
            WHERE a.StartTime >= @Today
              AND a.StartTime <  @Tomorrow
            ORDER BY a.StartTime ASC
            """;

            using var conn = CreateConnection();

            var kpiRaw = await conn.QuerySingleAsync(kpiSql);
            var queue = (await conn.QueryAsync<AppointmentQueueItem>(queueSql)).ToList();

            var response = new ReceptionistDashboardResponse
            {
                AppointmentsToday = BuildNumberStat((int)kpiRaw.AppointmentsToday_Count,(int)kpiRaw.AppointmentsToday_PreviousCount,TrendComparison.Yesterday),
                CheckedInNow = BuildSimpleStat((int)kpiRaw.CheckedInNow,TrendComparison.Live),
                NewPatientsToday = BuildNumberStat((int)kpiRaw.NewPatientsToday_Count,(int)kpiRaw.NewPatientsToday_PreviousCount,TrendComparison.Yesterday),
                TodayQueue = queue
            };

            return Result<ReceptionistDashboardResponse>.Ok(response);
        }
    }
}
