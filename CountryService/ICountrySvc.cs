using ModelService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CountryService
{
    public interface ICountrySvc
    {
        Task<List<CountryModel>> GetCountriesAsync();
    }
}
