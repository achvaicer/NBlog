using NBlog.Web.Application.Service.Entity;

namespace NBlog.Web.Application.Service
{
    public interface IUserService
    {
        void Save(User user);

        bool Exists(string id);

        User Get(string id);

        User Current { get; }
    }
}