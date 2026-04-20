using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin;
using Owin;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.Cookies;
using Microsoft.AspNet.Identity;


namespace Pawchase
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // 1. Setup Cookie Authentication
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login")
            });

            // 2. Allow the app to temporarily store user info during the login process
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // 3. Configure Google Authentication
            app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
            {
                ClientId = "YOUR_CLIENT_ID.apps.googleusercontent.com",
                ClientSecret = "YOUR_CLIENT_SECRET"
            });
        }
    }
}

