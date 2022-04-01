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
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace UserService
{
    public class UserSvc : IUserSvc
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHostingEnvironment _env;
        private readonly ApplicationDbContext _db;
        private readonly ICookieSvc _cookieSvc;
        private readonly IActivitySvc _activitySvc;
        private readonly IServiceProvider _provider;
        private readonly DataProtectionKeys _dataProtectionKeys;

        public UserSvc(UserManager<ApplicationUser> userManager, IHostingEnvironment env,
                        ApplicationDbContext db, ICookieSvc cookieSvc, IActivitySvc activitySvc,
                        IServiceProvider provider, IOptions<DataProtectionKeys> dataProtectionKeys)
        {
            _userManager = userManager;
            _env = env;
            _db = db;
            _cookieSvc = cookieSvc;
            _activitySvc = activitySvc;
            _provider = provider;
            _dataProtectionKeys = dataProtectionKeys.Value;
        }

        public async Task<ProfileModel> GetUserProfileById(string userId)
        {
            ProfileModel profile = new ProfileModel();

            var loggedUserId = GetLoggedInUserId();
            var user = await _userManager.FindByIdAsync(loggedUserId);

            if (user == null) return null;
            try
            {
                profile = new ProfileModel()
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Username = user.UserName,
                    Phone = user.PhoneNumber,
                    Birthday = user.Birthday,
                    Gender = user.Gender,
                    Displayname = user.DisplayName,
                    Firstname = user.FirstName,
                    Middlename = user.MiddleName,
                    LastName = user.LastName,
                    IsEmailVerified = user.EmailConfirmed,
                    IsPhoneVerified = user.PhoneNumberConfirmed,
                    IsTermsAccepted = user.Terms,
                    IsTwoFactorOn = user.TwoFactorEnabled,
                    ProfilePic = user.ProfilePic,
                    UserRole = user.UserRole,
                    IsAccountLocked = user.LockoutEnabled,
                    IsEmployee = user.IsEmployee,
                    UseAddress = new List<AddressModel>(await _db.Addresses.Where(x => x.UserId == user.Id).Select(n =>
                        new AddressModel()
                        {
                            AddressId = n.AddressId,
                            Line1 = n.Line1,
                            Line2 = n.Line2,
                            Unit = n.Unit,
                            Country = n.Country,
                            State = n.State,
                            City = n.City,
                            PostalCode = n.PostalCode,
                            Type = n.Type,
                            UserId = n.UserId
                        }).ToListAsync()),
                    Activities = new List<ActivityModel>(_db.Activities.Where(x => x.UserId == user.Id)).OrderByDescending(o => o.Date).Take(20).ToList()
                };
            }
            catch (Exception ex)
            {
                Log.Error("An error occurred while seeding the database  {Error} {StackTrace} {InnerException} {Source}",
                    ex.Message, ex.StackTrace, ex.InnerException, ex.Source);
            }

            return profile;
        }

        private string GetLoggedInUserId()
        {
            try
            {
                var protectorProvider = _provider.GetService<IDataProtectionProvider>();
                var protector = protectorProvider.CreateProtector(_dataProtectionKeys.ApplicationUserKey);
                var unprotectUserId = protector.Unprotect(_cookieSvc.Get("user_id"));
                return unprotectUserId;
            }
            catch (Exception ex)
            {
                Log.Error("An error occurred while seeding the database  {Error} {StackTrace} {InnerException} {Source}",
                    ex.Message, ex.StackTrace, ex.InnerException, ex.Source);
            }

            return null;
        }
    }
}
