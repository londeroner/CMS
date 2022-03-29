using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace CookieService
{
    public class CookieSvc : ICookieSvc
    {
        private readonly CookieOptions _cookieOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CookieSvc(CookieOptions cookieOptions, IHttpContextAccessor httpContextAccessor)
        {
            _cookieOptions = cookieOptions;
            _httpContextAccessor = httpContextAccessor;
        }

        public string Get(string key) => _httpContextAccessor.HttpContext.Request.Cookies[key];

        public string GetUserIP() 
        {
            return string.Empty;
        }

        public string GetUserCountry()
        {
            return string.Empty;
        }

        public string GetUserOS()
        {
            return string.Empty;
        }

        public void SetCookie(string key, string value, int? expireTime, bool isSecure, bool isHttpOnly)
        {
            _cookieOptions.Expires = expireTime.HasValue ? DateTime.Now.AddMinutes(expireTime.Value) : DateTime.Now.AddMilliseconds(10);
            _cookieOptions.Secure = isSecure;
            _cookieOptions.HttpOnly = isHttpOnly;
            _httpContextAccessor.HttpContext.Response.Cookies.Append(key, value, _cookieOptions);
        }

        public void SetCookie(string key, string value, int? expireTime) 
        {
            _cookieOptions.Secure = true;
            _cookieOptions.HttpOnly = true;
            _cookieOptions.Expires = expireTime.HasValue ? DateTime.Now.AddMinutes(expireTime.Value) : DateTime.Now.AddMilliseconds(10);
            _cookieOptions.SameSite = SameSiteMode.Strict;
            _httpContextAccessor.HttpContext.Response.Cookies.Append(key, value, _cookieOptions);
        }

        public void DeleteCookie(string key) 
        {
            _httpContextAccessor.HttpContext.Response.Cookies.Delete(key);
        }

        public void DeleteAllCookies(IEnumerable<string> cookiesToDelete) 
        {
            foreach (var key in cookiesToDelete)
                _httpContextAccessor.HttpContext.Response.Cookies.Delete(key);
        }
    }
}
