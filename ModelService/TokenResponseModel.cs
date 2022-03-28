using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ModelService
{
    public class TokenResponseModel
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string Role { get; set; }
        public string Username { get; set; }
        public string UserId { get; set; }
        public bool TwoFactorLoginOn { get; set; }
        public DateTime Expiration { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
        public ClaimsPrincipal Principal { get; set; }
        public ResponseStatusInfoModel ResponseInfo { get; set; }
    }
}
