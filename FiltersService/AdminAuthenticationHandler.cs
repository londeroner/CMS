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

            return Task.CompletedTask;
        }
    }
}
