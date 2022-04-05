using ModelService;
using System.Threading.Tasks;

namespace AuthService
{
    public interface IAuthSvc
    {
        Task<TokenResponseModel> Auth(LoginViewModel model);
    }
}
