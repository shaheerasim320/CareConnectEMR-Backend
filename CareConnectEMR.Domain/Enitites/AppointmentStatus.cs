using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Domain.Enitites
{
    public static class AppointmentStatus
    {
        public const string Scheduled = "Scheduled";
        public const string Confirmed = "Confirmed";
        public const string CheckedIn = "CheckedIn";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string NoShow = "NoShow";

        public static readonly string[] All = [Scheduled, Confirmed, CheckedIn, Completed, Cancelled, NoShow];

        public static bool CanTransitionTo(string current, string next) =>
        (current, next) switch
        {
            (Scheduled, Confirmed) => true,
            (Scheduled, Cancelled) => true,
            (Confirmed, CheckedIn) => true,
            (Confirmed, Cancelled) => true,
            (CheckedIn, Completed) => true,
            (CheckedIn, NoShow) => true,
            _ => false
        };

    }
}
