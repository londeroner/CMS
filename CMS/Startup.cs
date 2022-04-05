using ActivityService;
using AuthService;
using CookieService;
using DataService;
using FiltersService;
using FunctionalService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ModelService;
using System;
using System.Text;

namespace CMS
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });

            //----------------------------DBConnections
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(Configuration.GetConnectionString("DataBase_DEV"),
                x => x.MigrationsAssembly("CMS")));
            services.AddDbContext<DataProtectionKeysContext>(options => options.UseNpgsql(Configuration.GetConnectionString("DataProtection"),
                x => x.MigrationsAssembly("CMS")));

            //----------------------------Functional Service
            services.Configure<AdminUserOptions>(Configuration.GetSection("AdminUserOptions"));
            services.Configure<AppUserOptions>(Configuration.GetSection("AppUserOptions"));
            services.AddTransient<IFunctionalSvc, FunctionalSvc>();

            //----------------------------Default Identity Options
            var identityDefaultOptionsConf = Configuration.GetSection("IdentityDefaultOptions");
            services.Configure<IdentityDefaultOptions>(identityDefaultOptionsConf);
            var identityDefaultOptions = identityDefaultOptionsConf.Get<IdentityDefaultOptions>();

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = identityDefaultOptions.PasswordRequireDigit;
                options.Password.RequiredLength = identityDefaultOptions.PasswordRequierdLength;
                options.Password.RequireLowercase = identityDefaultOptions.PasswordRequireLowerCase;
                options.Password.RequireNonAlphanumeric = identityDefaultOptions.PasswordRequireNonAlphanumeric;
                options.Password.RequireUppercase = identityDefaultOptions.PasswordRequireUpperCase;
                options.Password.RequiredUniqueChars = identityDefaultOptions.PasswordRequierdUniqueChars;

                options.Lockout.AllowedForNewUsers = identityDefaultOptions.LockoutAllowedForNewUsers;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(identityDefaultOptions.LockoutDefaultLockoutTimeSpanMinutes);
                options.Lockout.MaxFailedAccessAttempts = identityDefaultOptions.LockoutMaxFailedAccessAttempts;

                options.User.RequireUniqueEmail = identityDefaultOptions.UserRequireUniqueEmail;

                options.SignIn.RequireConfirmedEmail = identityDefaultOptions.SignInRequireConfirmedemail;
            }).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

            //----------------------------App Settings
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            //----------------------------Data Protection Service
            var dataProtectionSection = Configuration.GetSection("DataProtectionKeys");
            services.Configure<DataProtectionKeys>(dataProtectionSection);
            services.AddDataProtection().PersistKeysToDbContext<DataProtectionKeysContext>();

            //----------------------------JWT Authentication
            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            services.AddAuthentication(o =>
            {
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = appSettings.ValidateIssuerSigningKey,
                    ValidateIssuer = appSettings.ValidateIssuer,
                    ValidateAudience = appSettings.ValidateAudience,
                    ValidIssuer = appSettings.Site,
                    ValidAudience = appSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };
            });

            //----------------------------Authentication Services
            services.AddTransient<IAuthSvc, AuthSvc>();
            services.AddTransient<IActivitySvc, ActivitySvc>();
            services.AddAuthentication("Administrator").AddScheme<AdminAuthenticationOptions, AdminAuthenticationHandler>("Admin", null);

            //----------------------------Cookie Service
            services.AddHttpContextAccessor();
            services.AddTransient<CookieOptions>();
            services.AddTransient<ICookieSvc, CookieSvc>();

            //----------------------------Razor Pages Runtime Service
            services.AddMvc().AddControllersAsServices().AddRazorRuntimeCompilation().SetCompatibilityVersion(CompatibilityVersion.Latest);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            if (!env.IsDevelopment())
            {
                app.UseSpaStaticFiles();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "areas",
                    pattern: "{area:exists}/{controller=Home}/{action=Index}/{Id?}");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseAngularCliServer(npmScript: "start");
                }
            });
        }
    }
}
