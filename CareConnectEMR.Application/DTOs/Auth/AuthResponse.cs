using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.DTOs
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
    }
}
