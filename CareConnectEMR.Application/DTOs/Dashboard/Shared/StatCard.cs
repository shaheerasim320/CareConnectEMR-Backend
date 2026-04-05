using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.DTOs.Dashboard.Shared
{
    public class StatCard
    {
        public int Count { get; set; }
        public int PreviousCount { get; set; }
        public decimal? TrendPercent => PreviousCount == 0 ? null : Math.Round(((decimal)(Count - PreviousCount) / PreviousCount) * 100, 1);
        public string TrendDirection => (TrendPercent == null || TrendPercent == 0) ? "neutral" : TrendPercent > 0 ? "up" : "down";
    }
}
