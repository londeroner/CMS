using CookieService;
using Microsoft.AspNetCore.Mvc;
using ModelService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProfileController : Controller
    {
        private readonly ICookieSvc _cookieSvc;
        private readonly IServiceProvider _provider;
        private readonly DataProtectionKeys _dataProtectionKeys;
        private readonly AppSettings appSettings;

        public ProfileController()
        {

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
