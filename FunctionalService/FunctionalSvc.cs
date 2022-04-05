using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ModelService;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FunctionalService
{
    public class FunctionalSvc : IFunctionalSvc
    {
        private readonly AdminUserOptions adminUserOptions;
        private readonly AppUserOptions appUserOptions;
        private readonly UserManager<ApplicationUser> userManager;

        public FunctionalSvc(IOptions<AppUserOptions> appUserOptions, IOptions<AdminUserOptions> adminUserOptions, UserManager<ApplicationUser> userManager)
        {
            this.adminUserOptions = adminUserOptions.Value;
            this.appUserOptions = appUserOptions.Value;
            this.userManager = userManager;
        }

        public async Task CreateDefaultAdminUser()
        {
            try
            {
                var admin = new ApplicationUser
                {
                    Email = adminUserOptions.Email,
                    UserName = adminUserOptions.Username,
                    EmailConfirmed = true,
                    ProfilePic = GetDefaultProfilePic(),
                    PhoneNumber = "1234567890",
                    PhoneNumberConfirmed = true,
                    FirstName = adminUserOptions.FirstName,
                    LastName = adminUserOptions.LastName,
                    UserRole = "Administrator",
                    IsActive = true,
                    UserAddresses = new List<AddressModel>
                    {
                        new AddressModel { Country = adminUserOptions.Country, Type = "Billing" },
                        new AddressModel { Country = adminUserOptions.Country, Type = "Shipping" }
                    }
                };

                var result = await userManager.CreateAsync(admin, adminUserOptions.Password);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Administrator");
                    Log.Information("Admin user created {UserName}", admin.UserName);
                }
                else
                    Log.Error("Error while creating user {Error}", string.Join(",", result.Errors));
            }
            catch (Exception e)
            {
                Log.Error("Error while creating user {Error} {StackTrace} {InnerException} {Source}", 
                    e.Message, e.StackTrace, e.InnerException, e.Source);
            }
        }

        public async Task CreateDefaultUser()
        {
            try
            {
                var user = new ApplicationUser
                {
                    Email = appUserOptions.Email,
                    UserName = appUserOptions.Username,
                    EmailConfirmed = true,
                    ProfilePic = GetDefaultProfilePic(),
                    PhoneNumber = "1234567890",
                    PhoneNumberConfirmed = true,
                    FirstName = appUserOptions.FirstName,
                    LastName = appUserOptions.LastName,
                    UserRole = "Administrator",
                    IsActive = true,
                    UserAddresses = new List<AddressModel>
                    {
                        new AddressModel { Country = appUserOptions.Country, Type = "Billing" },
                        new AddressModel { Country = appUserOptions.Country, Type = "Shipping" }
                    }
                };

                var result = await userManager.CreateAsync(user, appUserOptions.Password);

                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(user, "Customer");
                    Log.Information("Aoo user created {UserName}", user.UserName);
                }
                else
                    Log.Error("Error while creating user {Error}", string.Join(",", result.Errors));
            }
            catch (Exception e)
            {
                Log.Error("Error while creating user {Error} {StackTrace} {InnerException} {Source}",
                    e.Message, e.StackTrace, e.InnerException, e.Source);
            }
        }

        private string GetDefaultProfilePic()
        {
            return string.Empty;
        }
    }
}
