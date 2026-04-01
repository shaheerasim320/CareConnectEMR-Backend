using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.DTOs
{
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = null!;
         public string AccessToken { get; set; } = null!;
    }
}
