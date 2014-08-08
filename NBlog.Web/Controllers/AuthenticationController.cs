using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.Extensions.AttributeExchange;
using DotNetOpenAuth.OpenId.Extensions.SimpleRegistration;
using DotNetOpenAuth.OpenId.RelyingParty;
using NBlog.Web.Application;
using NBlog.Web.Application.Infrastructure;
using NBlog.Web.Application.Service;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Security;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using Facebook;
using NBlog.Web.Application.Service.Entity;

namespace NBlog.Web.Controllers
{
    public partial class AuthenticationController : LayoutController
    {
        public AuthenticationController(IServices services)
            : base(services)
        {
        }

        private static string ParseFacebookToken(string querystring)
        {
            var parametros = querystring.Split('&');
            return parametros.First(x => x.Contains("access_token")).Split('=').Last();
        }

        private static StreamReader DoWebRequest(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                var response = (HttpWebResponse)request.GetResponse();
                var reader = new StreamReader(response.GetResponseStream(), encoding: Encoding.ASCII);
                return reader;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public ActionResult Facebook()
        {

            var facebookAppID = ConfigurationManager.AppSettings["FacebookAppID"];
            var redirect = ConfigurationManager.AppSettings["FacebookRedirectUrl"];
            var facebookAppSecret = ConfigurationManager.AppSettings["FacebookAppSecret"];

            if (string.IsNullOrEmpty(Request.QueryString["access_token"]) && string.IsNullOrEmpty(Request.QueryString["code"]))
            {
                var url = String.Format(Services.Config.Current.Facebook.RequestCode, facebookAppID, redirect);
                return Redirect(url);
            }
            if (!string.IsNullOrEmpty(Request.QueryString["code"]))
            {
                var url = String.Format(Services.Config.Current.Facebook.RequestAccessToken, facebookAppID, redirect, facebookAppSecret, Request.QueryString["code"]);
                var token = ParseFacebookToken(DoWebRequest(url).ReadToEnd());

                var groupId = ConfigurationManager.AppSettings["FacebookGroupId"];
                var fbClient = new FacebookClient(token);
                dynamic groups = fbClient.Get("me/groups");
                foreach (dynamic group in (JsonArray)groups["data"])
                {
                    if (group.id != groupId) continue;
                    SetAuthCookie(token, true, token);
                    break;
                }
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public ActionResult Logout(string returnUrl)
        {
            FormsAuthentication.SignOut();

            var url = returnUrl.AsNullIfEmpty() ?? Url.Action("Index", "Home");
            return Redirect(url);
        }

        
        private void SetAuthCookie(string username, bool createPersistentCookie, string userData)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException("username");

            var authenticationConfig =
                (AuthenticationSection)WebConfigurationManager.GetWebApplicationSection("system.web/authentication");

            var timeout = (int)authenticationConfig.Forms.Timeout.TotalMinutes;
            var expiry = DateTime.Now.AddMinutes((double)timeout);

            FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(2,
              username,
              DateTime.Now,
              expiry,
              createPersistentCookie,
              userData,
              FormsAuthentication.FormsCookiePath);

            string encryptedTicket = FormsAuthentication.Encrypt(ticket);

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName)
            {
                Value = encryptedTicket,
                HttpOnly = true,
                Secure = authenticationConfig.Forms.RequireSSL
            };

            if (ticket.IsPersistent)
                cookie.Expires = ticket.Expiration;

            Response.Cookies.Add(cookie);
        }

        private string GetFriendlyName(IAuthenticationResponse authResponse)
        {
            string friendlyName = "";

            var sregResponse = authResponse.GetExtension<ClaimsResponse>();
            var axResponse = authResponse.GetExtension<FetchResponse>();

            if (sregResponse != null)
            {
                friendlyName =
                    sregResponse.FullName.AsNullIfEmpty() ??
                    sregResponse.Nickname.AsNullIfEmpty() ??
                    sregResponse.Email;
            }
            else if (axResponse != null)
            {
                var fullName = axResponse.GetAttributeValue(WellKnownAttributes.Name.FullName);
                var firstName = axResponse.GetAttributeValue(WellKnownAttributes.Name.First);
                var lastName = axResponse.GetAttributeValue(WellKnownAttributes.Name.Last);
                var email = axResponse.GetAttributeValue(WellKnownAttributes.Contact.Email);

                friendlyName =
                    fullName.AsNullIfEmpty() ??
                    ((!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName)) ? firstName + " " + lastName : null) ??
                    email;
            }

            if (string.IsNullOrEmpty(friendlyName))
                friendlyName = authResponse.FriendlyIdentifierForDisplay;

            return friendlyName;
        }
    }
}