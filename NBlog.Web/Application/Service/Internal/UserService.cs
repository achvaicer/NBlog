using System;
using System.Linq;
using System.Web;
using System.Web.Security;
using NBlog.Web.Application.Service.Entity;
using Facebook;
using System.Configuration;
using NBlog.Web.Application.Storage;

namespace NBlog.Web.Application.Service.Internal
{
    public class UserService : IUserService
    {
        private readonly IConfigService _configService;
        private readonly IRepository _repository;

        public UserService(IConfigService configService, IRepository repository)
        {
            _configService = configService;
            _repository = repository;

            var identity = HttpContext.Current.User.Identity;
            
            if (identity.IsAuthenticated)
            {
                var fbClient = new FacebookClient(identity.Name);
                dynamic me = fbClient.Get("me");

                Current = _repository.Single<User>(me.id);
            }
        }

        public User Current { get; private set; }

        public void Save(User user)
        {
            if (string.IsNullOrEmpty(user._id))
                throw new ArgumentNullException("user", "User must have an Facebook Id value to Save()");

            
            _repository.Save<User>(user);
        }

        public bool Exists(string id)
        {
            return _repository.Exists<User>(id);
        }

        public User Get(string id)
        {
            return _repository.Single<User>(id);
        }
    }
}