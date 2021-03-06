using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ModelService;
using Serilog;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DataService;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace FiltersService
{
    public class AdminAuthenticationHandler : AuthenticationHandler<AdminAuthenticationOptions>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IServiceProvider _serviceProvider;
        private readonly IdentityDefaultOptions _identityDefaultOptions;
        private readonly DataProtectionKeys _dataProtectionKeys;
        private readonly AppSettings _appSettings;
        private const string AccessToken = "access_token";
        private const string User_Id = "user_id";
        private const string Username = "username";
        private string[] UserRoles = new[] { "Administrator" };

        public AdminAuthenticationHandler(
            IOptionsMonitor<AdminAuthenticationOptions> options, 
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock,
            UserManager<ApplicationUser> userManager, IOptions<AppSettings> appSettings, 
            IOptions<DataProtectionKeys> dataProtectionKeys, IServiceProvider serviceProvider, 
            IOptions<IdentityDefaultOptions> identityDefaultOptions) : base(options, logger, encoder, clock)
        {
            _userManager = userManager;
            _appSettings = appSettings.Value;
            _identityDefaultOptions = identityDefaultOptions.Value;
            _serviceProvider = serviceProvider;
            _dataProtectionKeys = dataProtectionKeys.Value;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Cookies.ContainsKey(AccessToken) || !Request.Cookies.ContainsKey(User_Id))
            {
                Log.Error("No Access Token of User Id found.");
                return await Task.FromResult(AuthenticateResult.NoResult());
            }

            if (!AuthenticationHeaderValue.TryParse($"{"Bearer " + Request.Cookies[AccessToken]}",
                out AuthenticationHeaderValue headerValue))
            {
                Log.Error("Could not parse Token from Authentication Header.");
                return await Task.FromResult(AuthenticateResult.NoResult());
            }

            if (!AuthenticationHeaderValue.TryParse($"{"Bearer " + Request.Cookies[User_Id]}",
                out AuthenticationHeaderValue headerValueUID))
            {
                Log.Error("Could not parse User Id from Authentication Header.");
                return await Task.FromResult(AuthenticateResult.NoResult());
            }
            try
            {
                var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
                var handler = new JwtSecurityTokenHandler();

                TokenValidationParameters validationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidIssuer = _appSettings.Site,
                        ValidAudience = _appSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                var protectorProvider = _serviceProvider.GetService<IDataProtectionProvider>();

                var protector = protectorProvider.CreateProtector(_dataProtectionKeys.ApplicationUserKey);

                var decryptedUID = protector.Unprotect(headerValueUID.Parameter);

                var decryptedToken = protector.Unprotect(headerValue.Parameter);

                TokenModel token = new TokenModel();

                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContextService = scope.ServiceProvider.GetService<ApplicationDbContext>();
                    var userToken = dbContextService.Tokens.Include(x => x.User)
                        .FirstOrDefault(ut => ut.UserId == decryptedUID
                                           && ut.User.UserName == Request.Cookies[Username]
                                           && ut.User.Id == decryptedUID
                                           && ut.User.UserRole == "Administrator");
                    token = userToken;
                }

                if (token is null)
                    return await Task.FromResult(AuthenticateResult.Fail("You are not authorized to View this page"));

                IDataProtector layerTwoProtector = protectorProvider.CreateProtector(token.EncryptionKeyJwt);
                string decryptedTokenLayerTwo = layerTwoProtector.Unprotect(decryptedToken);

                var validateToken = handler.ValidateToken(decryptedTokenLayerTwo, validationParameters, out var securityToken);

                if (!(securityToken is JwtSecurityToken jwtSecurityToken) ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    return await Task.FromResult(AuthenticateResult.Fail("You are not authorized to View this page"));
                }

                var userName = validateToken.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;

                if (Request.Cookies[Username] != userName)
                    return await Task.FromResult(AuthenticateResult.Fail("You are not authorized to View this page"));

                var user = await _userManager.FindByNameAsync(userName);

                if (user is null || !UserRoles.Contains(user.UserRole))
                    return await Task.FromResult(AuthenticateResult.Fail("You are not authorized to View this page"));

                var identity = new ClaimsIdentity(validateToken.Claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return await Task.FromResult(AuthenticateResult.Success(ticket));
            }
            catch (Exception e)
            {
                Log.Error("An error has occured while seeding the database {Error} {StackTrace} {InnerException} {Source}",
                    e.Message, e.StackTrace, e.InnerException, e.Source);
                return await Task.FromResult(AuthenticateResult.Fail("Your are not authorized"));
            }
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("user_id");
            Response.Headers["WWW-Authenticate"] = $"Not authorized";
            Response.Redirect(_identityDefaultOptions.AccessDeniedPath);


            return Task.CompletedTask;
        }
    }
}
