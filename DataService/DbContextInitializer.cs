using CountryService;
using FunctionalService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataService
{
    public static class DbContextInitializer
    {
        public static async Task Initialize(DataProtectionKeysContext dataProtectionKeysContext, ApplicationDbContext applicationDbContext, 
            IFunctionalSvc functionalSvc, ICountrySvc countrySvc)
        {
            await dataProtectionKeysContext.Database.EnsureCreatedAsync();
            await applicationDbContext.Database.EnsureCreatedAsync();

            if (applicationDbContext.ApplicationUsers.Any())
                return;

            await functionalSvc.CreateDefaultAdminUser();
            await functionalSvc.CreateDefaultUser();

            var countries = await countrySvc.GetCountriesAsync();
            if (countries.Count > 0)
            {
                await applicationDbContext.Countries.AddRangeAsync(countries);
                await applicationDbContext.SaveChangesAsync();
            }
        }
    }
}
