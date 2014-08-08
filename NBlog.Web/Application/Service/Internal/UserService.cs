using System;
using System.Linq;
using System.Web;
using System.Web.Security;
using NBlog.Web.Application.Service.Entity;
using Facebook;

namespace NBlog.Web.Application.Service.Internal
{
    public class UserService : IUserService
    {
        private readonly IConfigService _configService;

        public UserService(IConfigService configService)
        {
            _configService = configService;

            var identity = HttpContext.Current.User.Identity;
            var isAdmin = false;

            var user = new User();

            if (identity.IsAuthenticated)
            {
                var fbClient = new FacebookClient(identity.Name);
                dynamic groups = fbClient.Get("me/groups");

                foreach (dynamic group in (JsonArray)groups["data"])
                {
                    if (group.id != configService.Current.GroupId) continue;
                    isAdmin = group.administrator ?? false;
                    break;
                }

                dynamic me = fbClient.Get("me");
                user.FacebookAccessToken = fbClient.AccessToken;
                user.FacebookId = me.id;
                user.FriendlyName = me.name;
                user.IsAdmin = isAdmin;
                user.IsAuthenticated = true;
                user.Username = me.name;
            }

            Current = user;
        }

        public User Current { get; private set; }
    }
}