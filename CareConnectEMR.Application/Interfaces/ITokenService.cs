using CareConnectEMR.Domain.Enitites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(ApplicationUser user,IList<string> roles);
        string GenerateRefreshToken();
        string? GetUserIdFromExpiredToken(string token);
    }
}
