using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionalService
{
    public interface IFunctionalSvc
    {
        Task CreateDefaultAdminUser();

        Task CreateDefaultUser();
    }
}
