using System;

namespace NBlog.Web.Application.Service.Entity
{
    public class User
    {
        public string Username { get; set; }
        public string FriendlyName { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool IsAdmin { get; set; }
        public string FacebookAccessToken { get; set; }
        public string FacebookId { get { return _id; } set { _id = value; } }
        public string _id { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime LastUpdated { get; set; }
        
         
    }
}