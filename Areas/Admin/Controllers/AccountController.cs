using AuthService;
using CookieService;
using DataService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using ModelService;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly AppSettings _appSettings;
        private readonly IServiceProvider _provider;
        private readonly ApplicationDbContext _db;
        private readonly IAuthSvc _authSvc;
        private readonly ICookieSvc _cookieSvc;
        private DataProtectionKeys _dataProtectionKeys;

        private const string AccessToken = "access_token";
        private const string User_Id = "user_id";
        string[] cookiesToDelete = { "twoFactorToken", "memberId", "rememberDevice", "user_id", "access_token" };

        public AccountController(IOptions<AppSettings> appSettings, IServiceProvider provider,
            ApplicationDbContext db, IAuthSvc authSvc, ICookieSvc cookieSvc, IOptions<DataProtectionKeys> dataProtectionKeys)
        {
            _appSettings = appSettings.Value;
            _provider = provider;
            _db = db;
            _authSvc = authSvc;
            _cookieSvc = cookieSvc;
            _dataProtectionKeys = dataProtectionKeys.Value;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            await Task.Delay(0);
            ViewData["ReturnUrl"] = returnUrl;
            try
            {
                if (!Request.Cookies.ContainsKey(AccessToken) || !Request.Cookies.ContainsKey(User_Id))
                {
                    return View();
                }
            }
            catch (Exception e)
            {
                Log.Error("An error occurred while seeding the database {Error} {StackTrace} {InnerException} {Source}",
                        e.Message, e.StackTrace, e.InnerException, e.Source);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody]LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                try
                {
                    var jwtToken = await _authSvc.Auth(model);
                    const int expireTime = 60;

                    _cookieSvc.SetCookie("access_token", jwtToken.Token, expireTime);
                    _cookieSvc.SetCookie("user_id", jwtToken.UserId, expireTime);
                    _cookieSvc.SetCookie("username", jwtToken.Username, expireTime);

                    Log.Information($"User {model.Email} logged in.");
                    return Ok("Success");
                }
                catch (Exception e)
                {
                    Log.Error("An error occurred while seeding the database {Error} {StackTrace} {InnerException} {Source}",
                        e.Message, e.StackTrace, e.InnerException, e.Source);
                }
            }

            ModelState.AddModelError("", "Invalid Username/Password was entered");
            return Unauthorized("Please check the login Credentials - Invalid Username/Password was entered");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = _cookieSvc.Get("user_id");

                if (userId is not null)
                {
                    var protectorProvider = _provider.GetService<IDataProtectionProvider>();
                    var protector = protectorProvider.CreateProtector(_dataProtectionKeys.ApplicationUserKey);
                    var unprotectedToken = protector.Unprotect(userId);

                    var rt = _db.Tokens.FirstOrDefault(t => t.UserId == unprotectedToken);

                    if (rt is not null)
                        _db.Tokens.Remove(rt);
                    await _db.SaveChangesAsync();

                    _cookieSvc.DeleteAllCookies(cookiesToDelete);
                }
            }
            catch (Exception e)
            {
                _cookieSvc.DeleteAllCookies(cookiesToDelete);
                Log.Error("An error occurred while seeding the database {Error} {StackTrace} {InnerException} {Source}",
                    e.Message, e.StackTrace, e.InnerException, e.Source);
            }

            Log.Information($"User logged out");
            return RedirectToLocal(null);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            return Url.IsLocalUrl(returnUrl) ? (IActionResult)Redirect(returnUrl) : RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }
}
