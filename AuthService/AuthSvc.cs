using ActivityService;
using CookieService;
using DataService;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using ModelService;
using Serilog;
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AuthService
{
    public class AuthSvc : IAuthSvc
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppSettings _appSettings;
        private readonly ApplicationDbContext _db;
        private readonly ICookieSvc _cookieSvc;
        private readonly IServiceProvider _provider;
        private readonly DataProtectionKeys _dataProtectionKeys;
        private readonly IActivitySvc _activitySvc;
        private IDataProtector _protector;
        private string[] UserRoles = new[] { "Administrator", "Customer" };
        private TokenValidationParameters validationParameters;
        private JwtSecurityTokenHandler handler;
        private string unprotectedToken;
        private ClaimsPrincipal validateToken;

        public AuthSvc(UserManager<ApplicationUser> userManager, IOptions<AppSettings> appSettings,
            IOptions<DataProtectionKeys> dataProtectionKeys, ApplicationDbContext db,
            ICookieSvc cookieSvc, IServiceProvider provider, IActivitySvc activitySvc)
        {
            _userManager = userManager;
            _appSettings = appSettings.Value;
            _dataProtectionKeys = dataProtectionKeys.Value;
            _db = db;
            _cookieSvc = cookieSvc;
            _provider = provider;
            _activitySvc = activitySvc;
        }

        public async Task<TokenResponseModel> Auth(LoginViewModel model)
        {
            ActivityModel activityModel = new ActivityModel();
            activityModel.Date = DateTime.UtcNow;
            activityModel.IpAddress = _cookieSvc.GetUserIP();
            activityModel.Location = _cookieSvc.GetUserCountry();
            activityModel.OperatingSystem = _cookieSvc.GetUserOS();

            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user is null) return CreateErrorResponseToken("Request not supported", HttpStatusCode.Unauthorized);

                var roles = await _userManager.GetRolesAsync(user);

                if (roles.FirstOrDefault() != "Administrator")
                {
                    activityModel.UserId = user.Id;
                    activityModel.Type = "Un-authorized Login Attempt";
                    activityModel.Icon = "fas fa-user-secret";
                    activityModel.Color = "danger";
                    await _activitySvc.AddUserActivity(activityModel);
                    Log.Error("Error : Role Not Admin");
                    return CreateErrorResponseToken("Request not supported", HttpStatusCode.Unauthorized);
                }

                if (!await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    activityModel.UserId = user.Id;
                    activityModel.Type = "Login attempt failed";
                    activityModel.Icon = "fas fa-times-circle";
                    activityModel.Color = "danger";
                    await _activitySvc.AddUserActivity(activityModel);
                    Log.Error("Error : Invalid password");
                    return CreateErrorResponseToken("Request not supported", HttpStatusCode.Unauthorized);
                }

                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    activityModel.UserId = user.Id;
                    activityModel.Type = "Login attempt success - Email not verified";
                    activityModel.Icon = "far fa-envelope";
                    activityModel.Color = "warning";
                    await _activitySvc.AddUserActivity(activityModel);
                    Log.Error("Error : Email not confirmed for {user}", user.UserName);
                    return CreateErrorResponseToken("Request not supported", HttpStatusCode.Unauthorized);
                }

                var authToken = await GenerateNewToken(user, model);
                activityModel.UserId = user.Id;
                activityModel.Type = "Login Attempt Successful";
                activityModel.Icon = "fas fa-thumbs-up";
                activityModel.Color = "success";
                await _activitySvc.AddUserActivity(activityModel);
                return authToken;
            }
            catch (Exception e)
            {
                Log.Error("An error has occurred while seeding the database {Error} {StackTrace} {InnerException} {Source}"
                    , e.Message, e.StackTrace, e.InnerException, e.Source);
            }

            return CreateErrorResponseToken("Request not supported", HttpStatusCode.Unauthorized);
        }

        private async Task<TokenResponseModel> GenerateNewToken(ApplicationUser user, LoginViewModel model)
        {
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_appSettings.Secret));

            var roles = await _userManager.GetRolesAsync(user);

            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
                    new Claim("LoggedOn", DateTime.Now.ToString(CultureInfo.InvariantCulture))
                }),

                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
                Issuer = _appSettings.Site,
                Audience = _appSettings.Audience,
                Expires = string.Equals(roles.FirstOrDefault(), "Administrator", StringComparison.CurrentCultureIgnoreCase) ?
                    DateTime.UtcNow.AddMinutes(60) : DateTime.UtcNow.AddMinutes(Convert.ToDouble(_appSettings.ExpireTime))
            };

            var encryptionKeyRt = Guid.NewGuid().ToString();
            var encryptionKeyJwt = Guid.NewGuid().ToString();

            var protectorProvider = _provider.GetService<IDataProtectionProvider>();

            var protectorJwt = protectorProvider.CreateProtector(encryptionKeyJwt);

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var encryptedToken = protectorJwt.Protect(tokenHandler.WriteToken(token));

            TokenModel newRtoken = CreateRefreshToken(_appSettings.ClientId, user.Id, Convert.ToInt32(_appSettings.RtExpireTime));

            newRtoken.EncryptionKeyJwt = encryptionKeyJwt;
            newRtoken.EncryptionKeyRt = encryptionKeyRt;
            
            try
            {
                var rt = _db.Tokens.FirstOrDefault(t => t.UserId == user.Id);

                if (rt != null)
                {
                    _db.Tokens.Remove(rt);

                    _db.Tokens.Add(newRtoken);
                }
                else await _db.Tokens.AddAsync(newRtoken);

                await _db.SaveChangesAsync(); 
            }
            catch (Exception e)
            {
                Log.Error("An error occurred while seeding the database {Error} {StackTrace} {InnerException} {Source}",
                    e.Message, e.StackTrace, e.InnerException, e.Source);
            }

            var protectorRt = protectorProvider.CreateProtector(encryptionKeyRt);
            var layerOneProtector = protectorProvider.CreateProtector(_dataProtectionKeys.ApplicationUserKey);

            var encAuthToken = new TokenResponseModel
            {
                Token = layerOneProtector.Protect(encryptedToken),
                Expiration = token.ValidTo,
                RefreshToken = protectorRt.Protect(newRtoken.Value),
                Role = roles.FirstOrDefault(),
                Username = user.UserName,
                UserId = layerOneProtector.Protect(user.Id),
                ResponseInfo = CreateResponse("Auth Token Created", HttpStatusCode.OK)
            };

            return encAuthToken;
        }

        private static TokenResponseModel CreateErrorResponseToken(string errorMessage, HttpStatusCode statusCode)
        {
            var errorToken = new TokenResponseModel
            {
                Token = null,
                Username = null,
                Role = null,
                RefreshTokenExpiration = DateTime.Now,
                RefreshToken = null,
                Expiration = DateTime.Now,
                ResponseInfo = CreateResponse(errorMessage, statusCode)
            };

            return errorToken;
        }

        private static ResponseStatusInfoModel CreateResponse(string errorMessage, HttpStatusCode statusCode)
        {
            var ResponseStatusInfo = new ResponseStatusInfoModel
            {
                Message = errorMessage,
                StatusCode = statusCode
            };

            return ResponseStatusInfo;
        }

        private static TokenModel CreateRefreshToken(string clientId, string userId, int expireTime)
        {
            return new TokenModel()
            {
                ClientId = clientId,
                UserId = userId,
                Value = Guid.NewGuid().ToString("N"),
                CreatedDate = DateTime.UtcNow,
                ExpiryTime = DateTime.UtcNow.AddMinutes(expireTime),
                EncryptionKeyRt = "",
                EncryptionKeyJwt = ""
            };
        }
    }
}
