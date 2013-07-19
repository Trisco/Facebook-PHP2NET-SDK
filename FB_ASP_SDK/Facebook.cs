using System;
using System.Collections.Generic;
using System.Web;

namespace facebook
{
    /**
    * Extends the BaseFacebook class with the intent of using
    * PHP sessions to store user ids and access tokens.
    */
    public class Facebook : FacebookBase
    {
        const string FBSS_COOKIE_NAME = "fbss";

        // We can set this to a high number because the main session
        // expiration will trump this.
        const long FBSS_COOKIE_EXPIRE = 31556926; // 1 year

        // Stores the shared session ID if one is set.
        protected string sharedSessionID;
        /**
        * Identical to the parent constructor
        *
        * @param Array $config the application configuration. Additionally
        * accepts "sharedSession" as a boolean to turn on a secondary
        * cookie for environments with a shared session (that is, your app
        * shares the domain with other apps).
        * @see BaseFacebook::__construct in facebook.php
        */
        public Facebook (FacebookConfig config): base(config)  {
            if (!string.IsNullOrWhiteSpace(config.sharedSession)) {
              this.initSharedSession();
            }
        }
        public Facebook()
            : base()
        {
        }

        /**
        * Provides the implementations of the inherited abstract
        * methods.  The implementation uses PHP sessions to maintain
        * a store for user ids and access tokens.
        */
        private List<String> _kSupportedKeys;
        private List<String> kSupportedKeys{
            get{
                if (_kSupportedKeys == null)
                {
                    _kSupportedKeys = new List<String>();
                    _kSupportedKeys.Add("state");
                    _kSupportedKeys.Add("code");
                    _kSupportedKeys.Add("access_token");
                    _kSupportedKeys.Add("user_id");
                }
                return _kSupportedKeys;
            }
            set{
                _kSupportedKeys = value;
            }
        }
        
        //protected dynamic data;
        protected void initSharedSession() {
            dynamic data;
            string cookie_name = this.getSharedSessionCookieName();
            if (HttpContext.Current.Request.Cookies[cookie_name] != null && !string.IsNullOrWhiteSpace(HttpContext.Current.Request.Cookies[cookie_name].Value))
            {
                data = this.parseSignedRequest(HttpContext.Current.Request.Cookies[this.getSignedRequestCookieName()].Value);
                if (data != null && !string.IsNullOrWhiteSpace(data["domain"]) && Facebook.isAllowedDomain(Http.getHttpHost(useTrustForwarded()), data["domain"]))
                {
                    //good case
                    this.sharedSessionID = data["id"];
                    return;
                }
            }

            // evil/corrupt/missing case
            string base_domain = this.getBaseDomain();
            this.sharedSessionID = Encryption.Md5Hash(new Guid().ToString());
            Dictionary<String, String> para = new Dictionary<string,string>();
            para.Add("domain", base_domain);
            para.Add("id", this.sharedSessionID);
            string cookie_value = this.makeSignedRequest(para);
            Http.setCookie(cookie_name, cookie_value, DateTime.Now.AddSeconds(Facebook.FBSS_COOKIE_EXPIRE), "/", base_domain);
        }

        protected override FacebookBase setPersistentData(String key, String value)
        {
            if (this.kSupportedKeys.Contains(key))
            {
                String session_var_name = constructSessionVariableName(key);
                if (HttpContext.Current.Session[session_var_name] == null)
                    HttpContext.Current.Session.Add(session_var_name, value);
                else
                    HttpContext.Current.Session[session_var_name] = value;
            }
            else
            {
                FacebookBase.errorLog("Unsupported key passed to setPersistentData.");                      
            }
            return this;
        }

        protected override Object getPersistentData(String key, Object _default = null)
        {
            if (_default == null) _default = string.Empty;
            if (!this.kSupportedKeys.Contains(key))
            {
                FacebookBase.errorLog("Unsupported key passed to getPersistentData.");       
                return _default;
            }

            String session_var_name = constructSessionVariableName(key);
            return (HttpContext.Current.Session[session_var_name] == null) ? _default : HttpContext.Current.Session[session_var_name];
        }

        protected override void clearAllPersistentData()
        {
            foreach (String key in this.kSupportedKeys)
            {
                this.clearPersistentData(key);                
            }
            if (!string.IsNullOrWhiteSpace(this.sharedSessionID)){
                this.deleteSharedSessionCookie();
            }
            //fix 2013-01-16
          //  HttpContext.Current.Session.Clear();
          //  HttpContext.Current.Session.Abandon();
        }
        protected void deleteSharedSessionCookie() {
            string cookie_name = this.getSharedSessionCookieName();
            if (HttpContext.Current.Request.Cookies[cookie_name] != null)
            {
                Http.setCookie(cookie_name, string.Empty, DateTime.Now.AddDays(-1d), "/" , this.getBaseDomain());
            }
        }

        protected string getSharedSessionCookieName() {
            return Facebook.FBSS_COOKIE_NAME +  '_' + this.getAppId();
        }
        protected override void clearPersistentData(String key)
        {
            if (kSupportedKeys.Contains(key))
            {
                String session_var_name = constructSessionVariableName(key);
                HttpContext.Current.Session.Remove(session_var_name);
            }
            else
            {
                FacebookBase.errorLog("Unsupported key passed to clearPersistentData.");                 
            }
        }

        protected String constructSessionVariableName(String key) {
            string res = "fb_" + this.getAppId() + "_" + key;
            if (!string.IsNullOrWhiteSpace(this.sharedSessionID))
                res = this.sharedSessionID + "_" + res;
            return res;
        }
    }
}
