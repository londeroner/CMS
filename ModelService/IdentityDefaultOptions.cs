using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelService
{
    public class IdentityDefaultOptions
    {
        public bool PasswordRequireDigit { get; set; }
        public bool PasswordRequireNonAlphanumeric { get; set; }
        public bool PasswordRequireUpperCase { get; set; }
        public bool PasswordRequireLowerCase { get; set; }
        public int PasswordRequierdLength { get; set; }
        public int PasswordRequierdUniqueChars { get; set; }

        public double LockoutDefaultLockoutTimeSpanMinutes { get; set; }
        public int LockoutMaxFailedAccessAttempts { get; set; }
        public bool LockoutAllowedForNewUsers { get; set; }

        public bool UserRequireUniqueEmail { get; set; }
        public bool SignInRequireConfirmedemail { get; set; }
        public string AccessDeniedPath { get; set; }
    }
}
