using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CookieService
{
    public interface ICookieSvc
    {
        public string Get(string key);
        public string GetUserIP();
        public string GetUserCountry();
        public string GetUserOS();
        public void SetCookie(string key, string value, int? expireTime, bool isSecure, bool isHttpOnly);
        public void SetCookie(string key, string value, int? expireTime);
        public void DeleteCookie(string key);
        public void DeleteAllCookies(IEnumerable<string> cookiesToDelete);
    }
}
