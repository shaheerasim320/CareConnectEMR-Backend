using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.DTOs.Appointment
{
    public class CancelAppointmentRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
