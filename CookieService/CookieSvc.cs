using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

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
            string userIP = "unknown";

            try
            {
                userIP = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            }
            catch (Exception ex)
            {
                Log.Error("An error occurred while seeding the database  {Error} {StackTrace} {InnerException} {Source}",
                    ex.Message, ex.StackTrace, ex.InnerException, ex.Source);
            }

            return userIP;
        }

        public string GetUserCountry()
        {
            try
            {
                string userIP = GetUserIP();
                string info = new WebClient().DownloadString("http://ipinfo.io/" + userIP);
                var ipInfo = JsonConvert.DeserializeObject<IPInfo>(info);
                RegionInfo regionInfo = new RegionInfo(ipInfo.Country);
                ipInfo.Country = regionInfo.EnglishName;

                if (!string.IsNullOrEmpty(userIP))
                    return ipInfo.Country;

            }
            catch (Exception ex)
            {
                Log.Error("An error occurred while seeding the database  {Error} {StackTrace} {InnerException} {Source}",
                    ex.Message, ex.StackTrace, ex.InnerException, ex.Source);
            }

            return "unknown";
        }

        public string GetUserOS()
        {
            string userOS = "unknown";

            try
            {
                userOS = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString();
            }
            catch (Exception ex)
            {
                Log.Error("An error occurred while seeding the database  {Error} {StackTrace} {InnerException} {Source}",
                    ex.Message, ex.StackTrace, ex.InnerException, ex.Source);
            }

            return userOS;
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
