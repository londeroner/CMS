using CookieService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ModelService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserService;

namespace CMS.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProfileController : Controller
    {
        private readonly ICookieSvc _cookieSvc;
        private readonly IServiceProvider _provider;
        private readonly DataProtectionKeys _dataProtectionKeys;
        private readonly AppSettings _appSettings;
        //private readonly IUserSvc _userSvc;

        public ProfileController(ICookieSvc cookieSvc, IServiceProvider provider,
            IOptions<DataProtectionKeys> dataProtectionKeys, IOptions<AppSettings> appSettings)
        {
            //_userSvc = userSvc;
            _cookieSvc = cookieSvc;
            _provider = provider;
            _dataProtectionKeys = dataProtectionKeys.Value;
            _appSettings = appSettings.Value;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Security()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Activity()
        {
            return View();
        }

    }
}
