using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace facebook
{
    public class example
    {
        public example()
        {

            FacebookConfig config = new FacebookConfig(){ appId = "117743971608120", apiSecret = "<secret>" };
            Facebook facebook = new Facebook(config);

            // Get User ID
            String user = facebook.getUser();

            // We may or may not have this data based on whether the user is logged in.
            //
            // If we have a $user id here, it means we know the user is logged into
            // Facebook, but we don't know if the access token is valid. An access
            // token is invalid if the user logged out of Facebook.
            FacebookResponse user_profile = null;
            if (!String.IsNullOrEmpty(user)) {
                try {
                    // Proceed knowing you have a logged in user who's authenticated.
                    user_profile = facebook._graph("/me");
                } catch (Exception e) {
                    user = null;
                }
            }
            String logoutUrl = null;
            String loginUrl = null;
            // Login or logout url will be needed depending on current user state.
            if (!String.IsNullOrEmpty(user)) {
                logoutUrl = facebook.getLogoutUrl();
            } else {
                loginUrl = facebook.getLoginUrl();
            }
        }
    }
}
