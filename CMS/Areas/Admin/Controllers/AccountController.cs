using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
