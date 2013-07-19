using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
//using System.Data.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Globalization;

/**
 * Based on  https://github.com/facebook/php-sdk/
 * WEB based applications ONLY!
 **/
namespace facebook
{

    public class FacebookApiException : Exception
    {
        /**
        * The result from the API server that represents the exception information.
        */
        protected static FacebookResponse _result { get; set; }
        protected FacebookResponse result { get; set; }
        protected static String _code { get; set; }
        /**
        * Make a new API Exception with the given result.
        *
        * @param Array $result the result from the API server
        */
        public FacebookApiException(FacebookResponse result)
            : base(getMessage(result))
        {
            this.result = result;

        }

        public static String getMessage(FacebookResponse result)
        {
            String msg = "Unknown Error. Check getResult()";
            _result = result;
            if (_result == null)
            {
                msg = "Not logged in";
                return msg;
            }
            if (_result.ContainsKey("error_description"))
            {// OAuth 2.0 Draft 10 style
                msg = _result["error_description"].ToString();
            }
            else if (_result.ContainsKey("error_msg"))
            {// Rest server style
                msg = _result["error_msg"].ToString();
            }
            else
            {
                try
                {// OAuth 2.0 Draft 00 style
                    msg = _result["error"]["message"].ToString();
                }
                catch { }
            }
            _code = ((_result.ContainsKey("error_code")) ? _result["error_code"].ToString() : null);
            return _code + " - " + msg;
        }

        /**
        * Return the associated result object returned by the API server.
        *
        * @returns Array the result from the API server
        */
        public dynamic getResult()
        {
            return this.result;
        }

        /**
        * Returns the associated type for the error. This will default to
        * 'Exception' when a type is not available.
        *
        * @return String
        */
        public String getType()
        {
            String type = "Exception";
            if (this.result != null && this.result.ContainsKey("error"))
            {
                dynamic error = this.result["error"].ToString();
                if (error.GetType().Name.Equals(typeof(String)))
                {
                    // OAuth 2.0 Draft 10 style
                    return error.ToString();
                }
                else
                {
                    // OAuth 2.0 Draft 00 style
                    try
                    {
                        return error["type"];
                    }
                    catch
                    {
                        return type;
                    }
                }
            }
            return type;
        }

        /**
        * To make debugging easier.
        *
        * @returns String the string representation of the error
        */
        public String __toString()
        {
            return this.getType() + ": " + this.Message;
        }
    }

    public abstract class FacebookBase
    {
        private const String VERSION = "3.2.2"; // port from php v3.1.1 > upgraded to 3.2.2 dd 2013-01-15
        /**
        * Signed Request Algorithm.
        */
        private const String SIGNED_REQUEST_ALGORITHM = "HMAC-SHA256";

        /**
        * Default options for HttpRequests.
        */
        public FacebookHttpRequestConfig httpRequestSettings;
        /**
        * List of query parameters that get automatically dropped when rebuilding
        * the current URL.
        */
        protected List<String> DropQueryParams;
        /**
        * Maps aliases to Facebook domains.
        */
        public Dictionary<String, String> DomainMap;

        /**
        * The ID of the facebook user, or null if th user is logged out
        */
        protected String user;

        /**
        * The data from the signed_request token.
        */
        protected dynamic signedRequest;

        /**
         * A CSRF state variable to asist in the defense against CSRF attacks
         */
        protected String state;
        /**
        * The OAuth access token received in exchange for a valid authorization
        * code.  null means the access token has yet to be determined.
        */
        protected String accessToken;
        /**
         * Configuration options for the user 
         */
        protected FacebookConfig config;

        /*
         *  addition Trisco: keep original json response 
         */
        protected string _jsonresult { get; set; }
        

        public FacebookBase()
        {
            this.config = new FacebookConfig();
        }

        public FacebookBase(FacebookConfig config)
        {
            this.config = config;
            if (this.config == null) this.config = new FacebookConfig();

            this.httpRequestSettings = new FacebookHttpRequestConfig();
            this.DropQueryParams = new List<String>();
            this.DropQueryParams.Add("code");
            this.DropQueryParams.Add("state");
            this.DropQueryParams.Add("signed_request");
            this.DomainMap = new Dictionary<String, String>();
            this.DomainMap.Add("api", "https://api.facebook.com/");
            this.DomainMap.Add("api_video", "https://api-video.facebook.com/");
            this.DomainMap.Add("api_read", "https://api-read.facebook.com/");
            this.DomainMap.Add("graph", "https://graph.facebook.com/");
            this.DomainMap.Add("www", "https://www.facebook.com/");
            this.user = null;
            this.accessToken = null;


            if (getPersistentData("state") != null) this.state = getPersistentData("state").ToString();
            else this.state = string.Empty;
            /*
            String cookieName = getCSRFTokenCookieName();
            if (HttpContext.Current.Response.Cookies[cookieName] != null && !String.IsNullOrEmpty(HttpContext.Current.Response.Cookies[cookieName].ToString()))
                this.state = HttpContext.Current.Response.Cookies[cookieName].Value;
             */
        }

        /**
        * Set the Application ID.
        *
        * @param String $appId the Application ID
        */
        public FacebookBase setAppId(String appId)
        {
            this.config.appId = appId;
            return this;
        }
        /**
        * Get the Application ID.
        *
        * @return String the Application ID
        */
        public String getAppId()
        {
            return this.config.appId;
        }
        /**
        * Set the API Secret.
        *
        * @param String $appId the API Secret
        */
        public FacebookBase setApiSecret(String apiSecret)
        {
            this.config.apiSecret = apiSecret;
            return this;
        }
        /**
        * Get the API Secret.
        *
        * @return String the API Secret
        */
        public String getApiSecret()
        {
            return this.config.apiSecret;
        }

        /**
        * Set the file upload support status.
        *
        * @param Boolean $fileUploadSupport
        */
        public FacebookBase setFileUploadSupport(Boolean fileUploadSupport)
        {
            this.config.fileUploadSupport = fileUploadSupport;
            return this;
        }

        /**
        * Get the file upload support status.
        *
        * @return Boolean
        */
        public Boolean useFileUploadSupport()
        {
            return this.config.fileUploadSupport;
        }

        /**
        * Set the trust HTTP_X_FORWARDED_* headers status.
        *
        * @param Boolean $trustForwarded
        */
        public FacebookBase setTrustForwarded(Boolean trustForwarded)
        {
            this.config.trustForwarded = trustForwarded;
            return this;
        }

        /**
        * Get the trust HTTP_X_FORWARDED_* headers status.
        *
        * @return Boolean
        */
        public Boolean useTrustForwarded()
        {
            return this.config.trustForwarded;
        }

        /**
        * Sets the access token for api calls.  Use this if you get
        * your access token by other means and just want the SDK
        * to use it.
        *
        * @param String $access_token an access token.
        */
        public FacebookBase setAccessToken(String access_token)
        {
            this.accessToken = access_token;
            return this;
        }

        /**
        * Extend an access token, while removing the short-lived token that might
        * have been generated via client-side flow. Thanks to http://bit.ly/b0Pt0H
        * for the workaround.
        */
        public string setExtendedAccessToken()
        {
            string access_token_response = string.Empty;
            Dictionary<String, String> parameters = new Dictionary<String, String>();
            try
            {
                // need to circumvent json_decode by calling _oauthRequest
                // directly, since response isn't JSON format.       
                parameters.Add("client_id", this.getAppId());
                parameters.Add("client_secret", this.getApiSecret());
                parameters.Add("grant_type", "fb_exchange_token");
                parameters.Add("fb_exchange_token", this.getAccessToken());
                access_token_response = this._oauthRequest(this.getUrl("graph", "/oauth/access_token"), parameters);
            }
            catch (FacebookApiException e)
            {
                // most likely that user very recently revoked authorization.
                // In any event, we don't have an access token, so say so.
                return null;
            }

            if (string.IsNullOrEmpty(access_token_response))
                return null;
            if (access_token_response.Substring(0, 1) == "?") access_token_response = access_token_response.Substring(1);
            string[] values = access_token_response.Split('&');
            foreach (string val in values)
            {
                string[] pair = val.Split('=');
                if (pair[0] == "access_token")
                {
                    this.clearAllPersistentData();
                    this.setPersistentData("access_token", pair[1]);
                    return pair[1];
                }
            }
            //if (!access_token_response.ContainsKey("oauth_token"))
            //if (!access_token_response.ContainsKey("access_token"))
            return null;
        }

        /**
        * Determines the access token that should be used for API calls.
        * The first time this is called, $this->accessToken is set equal
        * to either a valid user access token, or it's set to the application
        * access token if a valid user access token wasn't available.  Subsequent
        * calls return whatever the first call returned.
        *
        * @return String the access token
        */
        public String getAccessToken()
        {
            if (String.IsNullOrEmpty(this.accessToken))
            {
                // first establish access token to be the application
                // access token, in case we navigate to the /oauth/access_token
                // endpoint, where SOME access token is required.
                this.setAccessToken(this.getApplicationAccessToken());
                String user_access_token = this.getUserAccessToken();
                if (!String.IsNullOrEmpty(user_access_token))
                {
                    this.setAccessToken(user_access_token);
                }

            }
            return this.accessToken;
        }

        /**
        * Determines and returns the user access token, first using
        * the signed request if present, and then falling back on
        * the authorization code if present.  The intent is to
        * return a valid user access token, or false if one is determined
        * to not be available.
        *
        * @return String a valid user access token, or false if one
        *         could not be determined.
        */
        protected String getUserAccessToken()
        {
            // first, consider a signed request if it's supplied.
            // if there is a signed request, then it alone determines
            // the access token.
            FacebookResponse signed_request = this.getSignedRequest();
            String code = string.Empty;
            if (signed_request != null)
            {
                if (signed_request.ContainsKey("oauth_token"))
                {
                    String access_token = signed_request["oauth_token"].ToString();
                    setPersistentData("access_token", access_token);
                    return access_token;
                }
                if (signed_request.ContainsKey("code"))
                {
                    code = signed_request["code"].ToString();
          
                    if (!string.IsNullOrWhiteSpace(code) && code.Equals(this.getPersistentData("code"))) {
                        // short-circuit if the code we have is the same as the one presented
                        return this.getPersistentData("access_token").ToString();
                    }

                    String access_token = this.getAccessTokenFromCode(code);
                    if (!string.IsNullOrEmpty(access_token))
                    {
                        setPersistentData("code", code);
                        setPersistentData("access_token", access_token);
                        return access_token;
                    }
                }
                // signed request states there's no access token, so anything
                // stored should be cleared.
                clearAllPersistentData();
                return null; // respect the signed request's data, even
                // if there's an authorization code or something else
            }

            code = this.getCode();
            if (!String.IsNullOrEmpty(code) && !code.Equals(getPersistentData("code")))
            {
                String access_token = this.getAccessTokenFromCode(code);
                if (!String.IsNullOrEmpty(access_token))
                {
                    setPersistentData("code", code);
                    setPersistentData("access_token", access_token);
                    return access_token;
                }

                // code was bogus, so everything based on it should be invalidated.
                FacebookBase.errorLog("code was bogus (getAccessTokenFromCode returns null), so everything based on it should be invalidated", EventLogEntryType.Warning);                  
                clearAllPersistentData();
                return null;
            }

            // as a fallback, just return whatever is in the persistent
            // store, knowing nothing explicit (signed request, authorization
            // code, etc.) was present to shadow it (or we saw a code in $_REQUEST,
            // but it's the same as what's in the persistent store)
            return getPersistentData("access_token").ToString();
        }

        /**
        * Get the data from a signed_request token
        * @param HttpRequest.Request["signed_request"]
        * @return String the base domain
        */
        public FacebookResponse getSignedRequest()
        {
            if (this.signedRequest == null)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(HttpContext.Current.Request["signed_request"]))
                        this.signedRequest = this.parseSignedRequest(HttpContext.Current.Request["signed_request"]);
                    else if (HttpContext.Current.Request.Cookies[this.getSignedRequestCookieName()] != null && !string.IsNullOrWhiteSpace(HttpContext.Current.Request.Cookies[this.getSignedRequestCookieName()].Value))
                        this.signedRequest = this.parseSignedRequest(HttpContext.Current.Request.Cookies[this.getSignedRequestCookieName()].Value);
                }
                catch (Exception ex) { }
            }

            return this.signedRequest;
        }

        /**
        * Get the UID from the session, Or null if the facebook user is not connected 
        * @return String the UID if available
        */
        public String getUser()
        {
            if (string.IsNullOrEmpty(this.user) || this.user == "0") return this.getUserFromAvailableData();
            return this.user;
        }
        /**
        * Determines the connected user by first examining any signed
        * requests, then considering an authorization code, and then
        * falling back to any persistent store storing the user.
        *
        * @return Integer the id of the connected Facebook user, or 0
        * if no such user exists.
        */
        protected String getUserFromAvailableData()
        {
            // if a signed request is supplied, then it solely determines
            // who the user is.
            string user = string.Empty;
            FacebookResponse signed_request = this.getSignedRequest();
            if (signed_request != null)
            {
                if (signed_request.ContainsKey("user_id"))
                {
                    user = signed_request["user_id"].ToString();
                    if (!user.Equals(this.getPersistentData("user_id")))
                    {
                        this.clearAllPersistentData();
                    }
                    setPersistentData("user_id", signed_request["user_id"].ToString());
                    return user;
                }

                // if the signed request didn't present a user id, then invalidate
                // all entries in any persistent store.
                clearAllPersistentData();
                return ""; // or null...
            }
//after login: user should be in persistent storage
            user = getPersistentData("user_id", "").ToString();
            String persisted_access_token = getPersistentData("access_token").ToString();

            // use access_token to fetch user id if we have a user access_token, or if
            // the cached access token has changed.
            String access_token = this.getAccessToken();
            if (!String.IsNullOrEmpty(access_token) && !access_token.Equals(this.getApplicationAccessToken()) &&
                !(!String.IsNullOrEmpty(user) && user!="0" && persisted_access_token.Equals(access_token)))
            {
                user = this.getUserFromAccessToken();
                if (!String.IsNullOrEmpty(user) && user != "0")
                    setPersistentData("user_id", user);
                else
                    clearAllPersistentData();
            }

            return user;
        }

        /**
        * Get a Login URL for use with redirects. By default, full page redirect is
        * assumed. If you are using the generated URL with a window.open() call in
        * JavaScript, you can pass in display=popup as part of the $params.
        *
        * The parameters:
        * - next: the url to go to after a successful login
        * - cancel_url: the url to go to after the user cancels
        * - req_perms: comma separated list of requested extended perms
        * - display: can be "page" (default, full page) or "popup"
        *
        * @param Array $params provide custom parameters
        * @return String the URL for the login flow
        */
        //parameter "scope" can be array in PHP, not here: scope must be ","-separated string of the permissions
        public String getLoginUrl(Dictionary<String, String> parameters = null)
        {
            if (parameters == null) parameters = new Dictionary<String, String>();
            this.establishCSRFTokenState();
            String currentUrl = this.getCurrentUrl();

            Dictionary<String, String> localParams = new Dictionary<String, String>();
            localParams.Add("client_id", this.getAppId());
            localParams.Add("redirect_uri", currentUrl);
            localParams.Add("state", this.state);

            return getUrl(
                "www",
                "dialog/oauth",
                mergeDictionaries(localParams, parameters)
            );
        }

        private Dictionary<String, String> mergeDictionaries(Dictionary<String, String> dic1, Dictionary<String, String> dic2)
        {
            foreach (String key in dic2.Keys)
            {
                if (dic1.ContainsKey(key))
                    dic1[key] = dic2[key];
                else
                    dic1.Add(key, dic2[key]);
            }
            return dic1;
        }

        /**
        * Get a Logout URL suitable for use with redirects.
        *
        * The parameters:
        * - next: the url to go to after a successful logout
        *
        * @param Array $params provide custom parameters
        * @return String the URL for the logout flow
        */
        public String getLogoutUrl(Dictionary<String, String> parameters = null)
        {
            if (parameters == null) parameters = new Dictionary<String, String>();
            Dictionary<String, String> localParams = new Dictionary<String, String>();
            localParams.Add("next", this.getCurrentUrl());
            localParams.Add("access_token", this.getAccessToken());
            return this.getUrl(
                "www",
                "logout.php",
                mergeDictionaries(localParams, parameters)
            );
        }

        /**
       * Get a login status URL to fetch the status from Facebook.
       *
       * The parameters:
       * - ok_session: the URL to go to if a session is found
       * - no_session: the URL to go to if the user is not connected
       * - no_user: the URL to go to if the user is not signed into facebook
       *
       * @param array $params Provide custom parameters
       * @return string The URL for the logout flow
       */
        public String getLoginStatusUrl(Dictionary<String, String> parameters = null)
        {
            if (parameters == null) parameters = new Dictionary<String, String>();
            Dictionary<String, String> localParams = new Dictionary<String, String>();
            localParams.Add("api_key", this.getAppId());
            localParams.Add("no_session", this.getCurrentUrl());
            localParams.Add("no_user", this.getCurrentUrl());
            localParams.Add("ok_session", this.getCurrentUrl());
            localParams.Add("session_version", "3");
            return this.getUrl(
                "www",
                "extern/login_status.php",
                mergeDictionaries(localParams, parameters)
            );
        }
        /**
       * Make an API call.
       *
       * @return mixed The decoded response
       */
        /* polymorphic 
           useless >> we only support Graph API, so simplify.
         */
        public FacebookResponse api(String path) {
            return this._graph(path);
        }

        protected String getSignedRequestCookieName()
        {
            return "fbsr_" + this.getAppId();
        }

        /**
       * Constructs and returns the name of the coookie that potentially contain
       * metadata. The cookie is not set by the BaseFacebook class, but it may be
       * set by the JavaScript SDK.
       *
       * @return string the name of the cookie that would house metadata.
       */
        protected String getMetadataCookieName()
        {
            return "fbm_" + this.getAppId();
        }

        /**
        * Parses the metadata cookie that our Javascript API set
        *
        * @return  an array mapping key to value
        */
        protected Dictionary <string, string> getMetadataCookie() {
           Dictionary <string, string> metadata = new Dictionary <string, string>();
    
            string cookie_name = this.getMetadataCookieName();

            if (HttpContext.Current.Request.Cookies[cookie_name] == null) {
                return metadata ;
            }

            // The cookie value can be wrapped in "-characters so remove them
            string cookie_value = HttpContext.Current.Request.Cookies[cookie_name].Value.ToString().Trim().Replace("\"", string.Empty);

            if (string.IsNullOrWhiteSpace(cookie_value)) {
                return metadata;
            }
            string[] parts = cookie_value.Split('&');

            foreach (string part in parts) {
                string[] pair = part.Split('=');
                if (!string.IsNullOrWhiteSpace(pair[0])) {
                    metadata.Add(HttpUtility.UrlDecode(pair[0]), (pair.Length>1)?HttpUtility.UrlDecode(pair[1]):"");
                }
            }
            return metadata;
        }

        /**
        * Get the authorization code from the query parameters, if it exists,
        * and otherwise return false to signal no authorization code was
        * discoverable.
        *
        * @return mixed the authorization code, or false if the authorization
        * code could not be determined.
        */
        protected String getCode()
        {
            if (HttpContext.Current.Request["code"] != null)
            {
                if (!string.IsNullOrEmpty(this.state) && HttpContext.Current.Request["state"] != null && this.state.Equals(HttpContext.Current.Request["state"].ToString()))
                {
                    // CSRF state has done its job, so clear it
                    this.state = null;
                    //HttpContext.Current.Response.Cookies[getCSRFTokenCookieName()].Expires = DateTime.Now.AddDays(-1);
                    clearPersistentData("state");
                    return HttpContext.Current.Request["code"];
                }
                else
                {
                    //self::errorLog('CSRF state token does not match one provided.');
                    FacebookBase.errorLog("CSRF state token does not match one provided: " + this.state + " vs " + HttpContext.Current.Request["state"], EventLogEntryType.Warning);  
                    return null;
                }
            }
            return null;
        }

        /**
        * Retrieves the UID with the understanding that
        * $this->accessToken has already been set and is
        * seemingly legitimate.  It relies on Facebook's Graph API
        * to retreive user information and then extract
        * the user ID.
        *
        * @return Integer returns the UID of the Facebook user, or
        * 0 if the Facebook user could not be determined.
        */
        protected String getUserFromAccessToken()
        {
            FacebookResponse user_info = null;
            try
            {
                user_info = this._graph("/me");
                return user_info["id"].ToString();
            }
            catch (FacebookApiException e)
            {
                return ""; // or null...
            }
            catch (NoNullAllowedException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw e; //
                //throw new NoNullAllowedException(e.Message + "####" + String.Join("; ", user_info.Keys));
            }
        }

        /**
        * Returns the access token that should be used for logged out
        * users when no authorization code is available.
        *
        * @return String the application access token, useful for
        * gathering public information about users and applications.
        */
        protected String getApplicationAccessToken()
        {
            return this.config.appId + '|' + this.config.apiSecret;
        }

        /**
        * Lays down a CSRF state token for this process.  We
        * only generate a new CSRF token if one isn't currently
        * circulating in the domain's cookie jar.
        *
        * @return void
        */
        protected void establishCSRFTokenState()
        {
            if (string.IsNullOrEmpty(this.state))
            {
                this.state = Encryption.Md5Hash(Guid.NewGuid().ToString());
                this.setPersistentData("state", this.state);
                /* Http.setCookie(getCSRFTokenCookieName(),
                         this.state,
                         DateTime.Now.AddHours(1)); // sticks for an hour*/
            }
        }

        /**
        * Retreives an access token for the given authorization code
        * (previously generated from www.facebook.com on behalf of
        * a specific user).  The authorization code is sent to graph.facebook.com
        * and a legitimate access token is generated provided the access token
        * and the user for which it was generated all match, and the user is
        * either logged in to Facebook or has granted an offline access permission.
        *
        * @param String $code an authorization code.
        * @return mixed an access token exchanged for the authorization code, or
        * false if an access token could not be generated.
        */
        public String getAccessTokenFromCode(String code, String redirect_uri = null)
        {
            if (String.IsNullOrEmpty(code)) return null;

            if (String.IsNullOrEmpty(redirect_uri))
            {
                redirect_uri = this.getCurrentUrl();
            }

            string access_token_response = string.Empty; //FacebookResponse = null
            try
            {
                Dictionary<String, String> parameters = new Dictionary<String, String>();
                parameters.Add("client_id", this.getAppId());
                parameters.Add("client_secret", this.getApiSecret());
                parameters.Add("redirect_uri", this.getCurrentUrl());
                parameters.Add("code", code);
                // need to circumvent json_decode by calling _oauthRequest
                // directly, since response isn't JSON format.
                //access_token_response = this._oauthRequest("graph", "/oauth/access_token", parameters);
                access_token_response = this._oauthRequest(this.getUrl("graph", "/oauth/access_token"), parameters);
            }
            catch (FacebookApiException e)
            {
                // most likely that user very recently revoked authorization.
                // In any event, we don't have an access token, so say so.
                FacebookBase.errorLog("getAccessTokenFromCode FacebookApiException - most likely that user very recently revoked authorization: " + e.Message, EventLogEntryType.Error);
    //deviation from facebook PHP SDK: we throw this error instead of ignoring it.
    //            return null;
                throw e;
            }

            if (string.IsNullOrEmpty(access_token_response))
            {
                FacebookBase.errorLog("getAccessTokenFromCode - access_token_response IsNullOrEmpty");
                return null;
            }
            if (access_token_response.Substring(0, 1) == "?") access_token_response = access_token_response.Substring(1);
            string[] values = access_token_response.Split('&');
            foreach (string val in values)
            {
                string[] pair = val.Split('=');
                if (pair[0] == "access_token")
                    return pair[1];
            }
            //if (!access_token_response.ContainsKey("oauth_token"))
            //if (!access_token_response.ContainsKey("access_token"))
            FacebookBase.errorLog("getAccessTokenFromCode - access_token_response doesn't contain access_token: " + access_token_response);
            return null;

            //return access_token_response["access_token"].ToString();
        }

        /**
       * Invoke the Graph API.
       *
       * @param String $path the path (required)
       * @param String $method the http method (default 'GET')
       * @param Array $params the query/post data
       * @return the decoded response object
       * @throws FacebookApiException
       */
        public FacebookResponse _graph(String path)
        {
            return this._graph(path, "GET", null);
        }
        public FacebookResponse _graph(String path, Dictionary<String, String> parameters = null)
        {
            return this._graph(path, "GET", parameters);
        }
        public FacebookResponse _graph(String path, String method = "GET", Dictionary<String, String> parameters = null)
        {
            //reset original response
            this._jsonresult = string.Empty;
            if (parameters == null) parameters = new Dictionary<String, String>();

            if (parameters.ContainsKey("method"))
                parameters["method"] = method; // method override as we always do a POST
            else
                parameters.Add("method", method);

            //FacebookResponse result = _oauthRequest("graph", path, parameters);
            //try-catch JsonConvert.DeserializeObject >> _oauthRequest deserializes automatically
            //dynamic dResult = JsonConvert.DeserializeObject(_oauthRequest(this.getUrl("graph", path), parameters)) as FacebookResponse;
            String json = _oauthRequest(this.getUrl("graph", path), parameters);
            //set original json response
            //throw new NoNullAllowedException(json);
            FacebookResponse result = this.json_decode(json);
            this._jsonresult = json;

            // results are returned, errors are thrown
            /*
     if (is_array($result) && isset($result['error'])) {
      $this->throwAPIException($result);
      // @codeCoverageIgnoreStart
    }
             * */
            if (result == null)
                this.throwAPIException(result);

            return result;
        }

        public string getJsonResult()
        {
            return this._jsonresult;
        }

        public FacebookResponse json_decode(string json)
        {
            
            FacebookResponse fbresult = null;
            WebResponse resp;
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            else
            {
                object obj;

                try
                {
                    obj = JsonConvert.DeserializeObject(json, null, SerializerSettings);
                }
                catch (JsonSerializationException ex)
                {
                    throw new System.Runtime.Serialization.SerializationException(ex.Message, ex);
                }

                // If the object is a JToken we want to
                // convert it to dynamic, it if is any
                // other type we just return it.
                var jToken = obj as JToken;
                if (jToken != null)
                {
                    fbresult = ConvertJToken(jToken);
                }
            }
            return fbresult;
        }
        public String json_encode(Dictionary<String, String> data = null) // FacebookResponse data)
        {
            String resp = String.Empty;                
            if (data == null || data.Count == 0)
            {
                return resp;
            }
            else
            {
                try
                {
                    resp = JsonConvert.SerializeObject(data, Formatting.None, this.SerializerSettings);
                }
                catch (JsonSerializationException ex)
                {
                    throw new System.Runtime.Serialization.SerializationException(ex.Message, ex);
                }

            }
            return resp;
        }

        /**
        * Make a OAuth Request
        *
        * @param String $path the path (required)
        * @param Array $params the query/post data
        * @return the decoded response object
        * @throws FacebookApiException
        */
        //protected FacebookResponse _oauthRequest(String name, String path, Dictionary<String, String> parameters)
        protected string _oauthRequest(String url, Dictionary<String, String> parameters) //type FacebookResponse
        {
            // json_encode all params values that are not strings? >> not necessary
            if (!parameters.ContainsKey("access_token"))
                parameters.Add("access_token", this.getAccessToken());

            if (!parameters.ContainsKey("appsecret_proof"))
                parameters.Add("appsecret_proof", this.getAppSecretProof(parameters["access_token"]));

            // json_encode all params values that are not strings
            /*    foreach ($params as $key => $value) {
                  if (!is_string($value)) {
                    $params[$key] = json_encode($value);
                  }
                }
            */
            return this.makeRequest(url, parameters);
        }

        /**
        * Generate a proof of App Secret
        * This is required for all API calls originating from a server
        * It is a sha256 hash of the access_token made using the app secret
        *
        * @param string $access_token The access_token to be hashed (required)
        *
        * @return string The sha256 hash of the access_token
        */
        protected String getAppSecretProof(String access_token) {
            return Encryption.hash_hmac_sha256(access_token, this.getApiSecret()); ;
        }

        /**
        * Makes an HTTP request. This method can be overridden by subclasses if
        * developers want to do fancier things or use something other than curl to
        * make the request.
        *
        * @param string $url The URL to make the request to
        * @param array $params The parameters to use for the POST body
        * @param CurlHandler $ch Initialized curl handle
        *
        * @return string The response text
        */
        private string makeRequest(String requestUrl, Dictionary<String, String> parameters, byte[] postData = null) //type FacebookResponse
        {
            string qs = string.Empty;
            HttpWebRequest request;

            if (parameters.ContainsKey("method") && !string.IsNullOrEmpty(parameters["method"]) && parameters["method"] == "GET")
            {
                parameters.Remove("method");
                if (parameters.Count > 0) requestUrl += "?" + Http.http_build_query(parameters);
                request = HttpWebRequest.Create(requestUrl) as HttpWebRequest;
                request.Method = "GET"; // Set the http method GET, POST, etc.
            }
            else
            {
                parameters.Remove("method");
                request = HttpWebRequest.Create(requestUrl) as HttpWebRequest;
                request.Method = "POST";
            }
            //if (parameters.Count > 0) qs = Http.http_build_query(parameters);


            request.KeepAlive = true;
            var cachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
            request.CachePolicy = cachePolicy;
            request.Expect = null;

            request.AllowAutoRedirect = true;
            //request.Timeout = 1000;

            /*if (parameters.ContainsKey("ContentType"))
                request.ContentType = parameters["ContentType"]; 
            else
                request.ContentType = "application/x-www-form-urlencoded";
            if (parameters.ContainsKey("UserAgent"))
                request.UserAgent = parameters["UserAgent"];            
            */
            //add data...
            //string qs = Http.http_build_query(parameters);
            //request.ContentLength = qs.Length;
            if (request.Method == "POST")
            {
                //todo: untested
                if (this.useFileUploadSupport() && postData != null)
                {
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = postData.Length;
                    using (var dataStream = request.GetRequestStream())
                    {
                        dataStream.Write(postData, 0, postData.Length);
                    }
                }else if (this.useFileUploadSupport() && parameters.ContainsKey("source"))
                {
                    NameValueCollection nvc = new NameValueCollection();
                    string source = parameters["source"];
                    parameters.Remove("source");
                    try
                    {
                        Http.HttpUploadFile(request, @source, "source", "image/jpeg", parameters);
                    }
                    catch (WebException ex)
                    {
                        return null;
                    }
                }
                else if (parameters.Count > 0)
                {
                    request.ContentType = "application/x-www-form-urlencoded";
                    postData = Encoding.UTF8.GetBytes(Http.http_build_query(parameters));
                    request.ContentLength = postData.Length;
                    //using (var dataStream = request.GetRequestStream())
                    using (StreamWriter requestWriter = new StreamWriter(request.GetRequestStream()))
                    {
                        //dataStream.Write(postData, 0, postData.Length);
                        requestWriter.Write(Http.http_build_query(parameters));
                        requestWriter.Close();
                    }
                }
            }

            //WebResponse resp;
            HttpWebResponse resp;
            StreamReader sr; String responseData;
            try
            {
                resp = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException webException)
            {
                //transform to FacebookException
                FacebookApiException e;
                //set deafult for unknown error
                String error = "{\"error_code\": " + webException.Status.ToString() + ",\"error\": { \"message\": " + webException.Message + ",\"type\": \"WebRequestException\",\"code\": \"WebRequestException\"}}";
                if (webException.Response != null)
                {
                    sr = new StreamReader(webException.Response.GetResponseStream());
                    responseData = sr.ReadToEnd();
                    if (!string.IsNullOrEmpty(responseData))    
                        error = responseData;
                }
                e = new FacebookApiException(this.json_decode(error));
                //optional for further handling...
                if (webException.Status == WebExceptionStatus.ProtocolError)
                {     
                    switch (((HttpWebResponse)webException.Response).StatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            throw e;
                        case HttpStatusCode.Unauthorized:
                            throw e;
                        case HttpStatusCode.BadRequest:
                            throw e;
                        case HttpStatusCode.Forbidden:
                            throw e;
                        case HttpStatusCode.InternalServerError:
                            throw e;
                        default: 
                            throw e;
                    } 
                }
                throw e; // FacebookApiException from webException;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            sr = new StreamReader(resp.GetResponseStream());
            responseData = sr.ReadToEnd();
            if (string.IsNullOrEmpty(responseData))
            {
                return null;
            }
            else
            {
                return responseData;
            }

        }

        /**
        * Parses a signed_request and validates the signature.
        * Then saves it in $this->signed_data
        *
        * @param String A signed token
        * @param Boolean Should we remove the parts of the payload that
        *                are used by the algorithm?
        * @return Array the payload inside it or null if the sig is wrong
        */
        protected FacebookResponse parseSignedRequest(String signed_request)
        {
            List<String> splitted = new List<String>(signed_request.Split('.'));
            String encoded_sig = splitted[0];
            splitted.RemoveAt(0);
            String payload = String.Join(".", splitted);

            //// decode the data
            ////byte[] sig = Encryption.Base64UrlDecode(encoded_sig);
            ////byte[] payloadB = Encryption.Base64UrlDecode(payload);
            String sig = Encryption.Base64_Url_Decode(encoded_sig);
            var dataString = Encryption.Base64_Url_Decode(payload);
            //dataString = dataString.Replace("\\", "");
            /*dynamic data1 = null;
            try
            {
                data1 = JsonConvert.DeserializeObject<dynamic>(payload, SerializerSettings);
            }
            catch
            {
                data1 = null;
            }*/
            var data = JObject.Parse(dataString);
            FacebookResponse dataDic = ConvertJToken(data);
            String encType = dataDic["algorithm"].value.ToString();
            if (!encType.ToUpper().Equals(FacebookBase.SIGNED_REQUEST_ALGORITHM))
            {
                FacebookBase.errorLog("Unknown algorithm. Expected " + FacebookBase.SIGNED_REQUEST_ALGORITHM);
                return null;
            }

            // check sig
            //TODO http://stackoverflow.com/questions/699041/hmac-sha256-in-php-and-c-differ
            // I suspect that the raw set to true in would have given the same results in php as asp in that topic
            // so no need to convert to hex;
            ////byte[] apiSecret = Encoding.UTF8.GetBytes(this.getApiSecret());
            ////HMACSHA256 hmacsha256 = new HMACSHA256(apiSecret);
            ////hmacsha256.ComputeHash(payloadB);
            ////String expected_sig = System.Text.Encoding.ASCII.GetString(hmacsha256.Hash);
            /*
                var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(this.getApiSecret()));
                byte[] digest = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var expected_sig = Encoding.UTF8.GetString(hmacsha256.Hash);
                */
            string expected_sig = Encryption.hash_hmac_sha256(payload, this.getApiSecret());
            if (!sig.Equals(expected_sig))
            {
                //self::errorLog('Bad Signed JSON signature!');
                FacebookBase.errorLog("Bad Signed JSON signature!");                
                return null;
            }

            return dataDic;
        }

        /**
        * Makes a signed_request blob using the given data.
        *
        * @param array The data array.
        * @return string The signed request.
        */
        protected String makeSignedRequest(Dictionary<String, String> data = null) {
            //if (parameters == null) parameters = new Dictionary<String, String>();
            if (data == null) {
                //throw new ArgumentException("makeSignedRequest expects a dictionary.");
                 data = new Dictionary<String, String>();
            }
            data.Add("algorithm", FacebookBase.SIGNED_REQUEST_ALGORITHM);
            //CultureInfo ci = new CultureInfo("nl-BE");   
            data.Add("issued_at", DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:sszzz", DateTimeFormatInfo.InvariantInfo)); //todo: check DateTimeFormatInfo.CurrentInfo?
          
            string json = this.json_encode(data);

            byte[] encodedBytes = Encoding.UTF8.GetBytes(json);
            string b64 = Convert.ToBase64String(encodedBytes);
            string raw_sig = Encryption.hash_hmac_sha256(b64, this.getApiSecret());
            string sig = Encryption.base64UrlEncode(raw_sig);

            return sig+'.'+b64;
        }

        /**
        * Build the URL for given domain alias, path and parameters.
        *
        * @param $name String the name of the domain
        * @param $path String optional path (without a leading slash)
        * @param $params Array optional query parameters
        * @return String the URL for the given parameters
        */
        protected String getUrl(String name, String path = "", Dictionary<String, String> parameters = null)
        {
            if (parameters == null) parameters = new Dictionary<String, String>();

            String url = this.DomainMap[name];
            if (!String.IsNullOrEmpty(path))
            {
                if (path.Substring(0, 1).Equals("/"))
                {
                    path = path.Substring(1);
                }
                url += path;
            }
            if (parameters.Count > 0)
            {
                url += "?" + Http.http_build_query(parameters);
            }
            return url;
        }

        /**
        * Get the base domain used for the cookie.
        */
        protected String getBaseDomain() {
            // The base domain is stored in the metadata cookie if not we fallback
            // to the current hostname
            Dictionary<String, String> metadata = this.getMetadataCookie();
            if (metadata.Count > 0 && metadata.ContainsKey("base_domain") && !string.IsNullOrWhiteSpace(metadata["base_domain"]))
            {
                return metadata["base_domain"].Trim().Replace(".", string.Empty);
            }
            return Http.getHttpHost(useTrustForwarded());
        }

        /**
        * Returns the Current URL, stripping it of known FB parameters that should
        * not persist.
        *
        * @return String the current URL
        */
        protected String getCurrentUrl()
        {
            //todo: cleanup: currently two different approches mixed.
            String currentUrl;
            String protocol = Http.getHttpProtocol(useTrustForwarded()) + "://";
            String host = Http.getHttpHost(useTrustForwarded());
           
            if (String.IsNullOrWhiteSpace(this.config.appDomain))
                currentUrl = protocol + HttpContext.Current.Request.ServerVariables["HTTP_HOST"] + HttpContext.Current.Request.ServerVariables["SCRIPT_NAME"];
            else
                currentUrl = protocol + this.config.appDomain + HttpContext.Current.Request.ServerVariables["SCRIPT_NAME"];
            
            if (!string.IsNullOrEmpty(HttpContext.Current.Request.ServerVariables["QUERY_STRING"]))
                currentUrl += "?" + HttpContext.Current.Request.ServerVariables["QUERY_STRING"];

            NameValueCollection parts = HttpUtility.ParseQueryString(HttpContext.Current.Request.ServerVariables["QUERY_STRING"]);
            //this.DropQueryParams

            Dictionary<String, String> partsQuery = new Dictionary<String, String>();
            foreach (String key in parts.AllKeys)
            {
                //todo: double check
                //ParseQueryString will split default.aspx?perms ==> key = null, value = 'perms' which is wrong
                //so we do a weird fix
                if (String.IsNullOrEmpty(key))
                {
                    String[] keys = parts[key].Split(',');
                    foreach (String k in keys)
                    {
                        //protected function shouldRetainParam($param)  is a single method here
                        if (!String.IsNullOrEmpty(k) && !this.DropQueryParams.Contains(k))
                            partsQuery.Add(k, null);
                    }
                }
                else if (!this.DropQueryParams.Contains(key) && parts[key] != null)
                    partsQuery.Add(key, parts[key].ToString());
            }
            String query = ((partsQuery.Count > 0) ? "?" : "") + Http.http_build_query(partsQuery);
            Uri MyUrl = HttpContext.Current.Request.Url;

            // use port if non default
            String port =
                ((protocol.Equals("http://") && MyUrl.Port != 80) ||
                (protocol.Equals("https://") && MyUrl.Port != 443))
                ? ":" + MyUrl.Port : "";

            // rebuild
            if (String.IsNullOrWhiteSpace(this.config.appDomain))
                return protocol + host + port + MyUrl.LocalPath + query; //return protocol + MyUrl.Host + port + MyUrl.LocalPath + query;
            else
                return protocol + this.config.appDomain + port + MyUrl.LocalPath + query;
        }

        //protected function shouldRetainParam($param)  is a single method in .NET

        /**
        * Analyzes the supplied result to see if it was thrown
        * because the access token is no longer valid.  If that is
        * the case, then the persistent store is cleared.
        *
        * @param $result a record storing the error message returned
        *        by a failed API call.
        */
        protected void throwAPIException(FacebookResponse result)
        {
            FacebookApiException e = new FacebookApiException(result);
            switch (e.getType())
            {
                // OAuth 2.0 Draft 00 style
                case "OAuthException":
                // OAuth 2.0 Draft 10 style
                case "invalid_token":
                    String message = e.Message;
                    if (message.Contains("Error validating access token") || message.Contains("Invalid OAuth access token"))
                    {
                        this.setAccessToken(null);
                        this.user = ""; // or null
                        clearAllPersistentData();
                    }
                    break;
            }

            throw e;
        }

        /**
        * Prints to the error log if you aren't in command line mode.
        *
        * @param string $msg Log message
        */
        protected static void errorLog(string msg, EventLogEntryType errorType = EventLogEntryType.Warning)
        {
            //do something with msg >> write to serverlog?
            /*if (!EventLog.SourceExists("facebook-asp-sdk"))
                EventLog.CreateEventSource("facebook-asp-sdk", "Application");
            EventLog.WriteEntry("facebook-asp-sdk", msg, EventLogEntryType.Error);
            */
            EventLog log = new EventLog();
            log.Source = "Application";
            log.WriteEntry(msg, errorType);
        }
        
        /**
        * Destroy the current session
        */
        public void destroySession() {
            this.accessToken = string.Empty;
            this.signedRequest = null;
            this.user = string.Empty;
            this.clearAllPersistentData();

            // Javascript sets a cookie that will be used in getSignedRequest that we
            // need to clear if we can
            try
            {
                string cookie_name = this.getSignedRequestCookieName();
                if (HttpContext.Current.Request.Cookies[cookie_name] != null)
                {
                    Http.setCookie(cookie_name, string.Empty, DateTime.Now.AddDays(-1d), "/", this.getBaseDomain());
                }
            }
            catch (Exception) { }
        }

        private JsonSerializerSettings SerializerSettings
        {
            get
            {
                var isoDate = new IsoDateTimeConverter();
                isoDate.DateTimeFormat = "yyyy-MM-ddTHH:mm:sszzz";
                var settings = new JsonSerializerSettings();
                settings.Converters = settings.Converters ?? new List<JsonConverter>();
                settings.Converters.Add(isoDate);
                settings.MissingMemberHandling = MissingMemberHandling.Ignore;
                settings.NullValueHandling = NullValueHandling.Include;
                settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                settings.TypeNameHandling = TypeNameHandling.None;
                settings.ConstructorHandling = ConstructorHandling.Default;
                return settings;
            }
        }

        private FacebookResponse ConvertJToken(JToken token, FacebookResponse response = null, int depth = 0)
        {
            if (response == null) response = new FacebookResponse();
            
            if (token == null)
            {
                response.original = "";
                response.depth = 0;
                return null;
            }
            response.original = token.ToString();
            response.depth = depth;

            var jValue = token as JValue;
            //keep original json string 
            if (jValue != null)
            {
                response.value = jValue.Value;
                return response;
            }

            var jContainer = token as JArray;
            if (jContainer != null)
            {
                foreach (JToken arrayItem in jContainer)
                {
                    response.AddChild(ConvertJToken(arrayItem, null, depth));
                }
                return response;
            }
            depth++;
            foreach (IEnumerable child in token.Children())
            {
                if (child.GetType().Name == typeof(JProperty).Name)
                {
                    JProperty childToken = (JProperty)child;
                    response.Add(childToken.Name, ConvertJToken(childToken.Value, null, depth));
                }
            }
            return response;
        }

        protected static bool isAllowedDomain(string big, string small) {
            if (big == small) {
                return true;
            }
            return FacebookBase.endsWith(big, '.'+small);
        }

        protected static bool endsWith(string big, string small) {
            int len = small.Length;
            if (len == 0) {
                return true;
            }
            return big.Substring(-len) == small;
        }

        /**
        * Each of the following three methods should be overridden in
        * a concrete subclass, as they are in the provided Facebook class.
        * The Facebook class uses PHP sessions to provide a primitive
        * persistent store, but another subclass--one that you implement--
        * might use a database, memcache, or an in-memory cache.
        *
        * @see Facebook
        */
        abstract protected FacebookBase setPersistentData(String key, String value);
        abstract protected Object getPersistentData(String key, Object _default = null);
        abstract protected void clearPersistentData(String key);
        abstract protected void clearAllPersistentData();
    }

    public class FacebookHttpRequestConfig
    {
        public Int32 ConnectionTimeOut { get; set; }
        public Boolean ReturnTransfer { get; set; }
        public Int32 TimeOut { get; set; }
        public String UserAgent { get; set; }
        public FacebookHttpRequestConfig()
        {
            ConnectionTimeOut = 10;
            ReturnTransfer = false;
            TimeOut = 60;
            UserAgent = "facebook-asp-2.0";
        }
    }
    public class FacebookConfig
    {
        /**
        * The Application ID.
        */
        public String appId { get; set; }

        /**
        * The Application API Secret.
        */
        public String apiSecret { get; set; }

        /**
        * The Application domain - if left blank ==> use the domain from facebook developer app settings; .
        */
        public String appDomain { get; set; }

        /**
        * Indicates if the CURL based @ syntax for file uploads is enabled.
        *
        * @var boolean
        */
        public Boolean fileUploadSupport { get; set; }

        /**
        * Indicates if we trust HTTP_X_FORWARDED_* headers.
        *
        * @var boolean
        */
        public Boolean trustForwarded { get; set; }

        public String sharedSession { get; set; }

        public FacebookConfig()
        {
            appId = string.Empty;
            apiSecret = string.Empty;
            fileUploadSupport = false;
            appDomain = string.Empty;
            trustForwarded = false;
            sharedSession = string.Empty;
        }
    }
    public class FacebookSession : ICloneable
    {
        //public String uid{get;set;}
        //public String access_token{get;set;}
        //public String sig{get;set;}
        public Dictionary<String, String> data;
        public FacebookSession()
        {
            data = new Dictionary<String, String>();
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class FacebookResponse : Dictionary<String, FacebookResponse>
    {
        public object value { get; set; }
        private Int32 idx { get; set; }

        //for debugging purposes
        public string original { get; set; }
        public int depth { get; set; }

        public override String ToString()
        {
            //should mymic "original" json output!
            //return original;
            if (this.Values.Count == 0)
            {
                return this.value.ToString();
            }
            else
            {
                StringBuilder resp = new StringBuilder("{\r\n");
                int t = 0;
                foreach (KeyValuePair<String, FacebookResponse> entry in this)
                {
                    if (t > 0) resp.Append(",\r\n");
                    for (int i = 0; i <= this.depth; i++) resp.Append("  "); //resp.Append("\t");

                    resp.Append("\"" + entry.Key + "\": ");
                    if (entry.Value.Count > 0)
                        resp.Append(entry.Value.ToString());
                    else
                    {
                        //crappy facebook graph API coders returning null, which was/is against specs!
                        if (entry.Value.value == null) resp.Append("null");
                        else resp.Append(entry.Value.value.ToString());
                    }
                    t++;
                }
                resp.Append("\r\n");
                for (int i = 0; i < this.depth; i++) resp.Append("  ");
                resp.Append("}");

                return resp.ToString();// original;  
            }
        }

        public FacebookResponse AddChild()
        {
            if(idx == null)
                idx = 0;
            String newIdx = idx.ToString();
            this.Add(idx.ToString(), new FacebookResponse());
            idx++;
            return this[newIdx];
        }

        public FacebookResponse AddChild(FacebookResponse value)
        {
            if (idx == null)
                 idx = 0;
            String newIdx = idx.ToString();
            this.Add(idx.ToString(), value);
            idx++;
            return this[newIdx];
        }
    }
}
