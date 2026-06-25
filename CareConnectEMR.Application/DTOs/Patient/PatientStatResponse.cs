using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.DTOs.Patient
{
    public class PatientStatResponse
    {
        public int? TotalPatients { get; set; }
        public int? RegisteredToday { get; set; }
        public int? IncompleteRecords { get; set; }
        public int? PatientsWaiting { get; set; }
        public int? SeenToday { get; set; }
        public int? FollowUpsDue { get; set; }
    }
}
