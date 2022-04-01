using ModelService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserService
{
    public interface IUserSvc
    {
        Task<ProfileModel> GetUserProfileById(string userId);
    }
}
