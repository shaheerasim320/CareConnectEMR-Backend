using CareConnectEMR.Application.Enums;
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
        public decimal? TrendValue {get; set; }
        public TrendType? TrendType { get; set; }
        public TrendDirection? TrendDirection { get; set; }
        public TrendComparison TrendComparison { get; set; }
    }
}
