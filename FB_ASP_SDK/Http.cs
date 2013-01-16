using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web;
using System.Net;
using System.IO;
using System.Collections.Specialized;

namespace facebook
{
    public static class Http
    {

        public static String http_build_query<T>(T obj, char seperator = '&')
        {
            Type firstType = obj.GetType();
            List<String> sb = new List<String>();
            foreach (PropertyInfo propertyInfo in firstType.GetProperties())
            {
                object firstValue = propertyInfo.GetValue(obj, null);
                if (firstValue == null)
                    sb.Add(propertyInfo.Name);
                else
                    sb.Add(propertyInfo.Name + "=" + firstValue);
            }
            String query = String.Join(seperator.ToString(), sb);
            query = HttpUtility.UrlEncode(query);
            return query;
        }
        public static String http_build_query(Dictionary<String, String> obj, char seperator = '&')
        {
            List<String> sb = new List<String>();
            foreach (String key in obj.Keys)
            {
                if (String.IsNullOrEmpty(obj[key]))
                    sb.Add(key);
                else
                    sb.Add(key + "=" + HttpUtility.UrlEncode(obj[key]));
            }
            String query = String.Join(seperator.ToString(), sb);
            return query;
        }
        public static Dictionary<String,String> RequestStringToDictionary(String request, char seperator = '&')
        {
            List<String> parameters = new List<String>(request.Split(seperator));
            Dictionary<String, String> dic = new Dictionary<String, String>();
            foreach (String parameter in parameters)
            {
                List<String> t = new List<String>(parameter.Split('='));
                String key = t[0];
                t.RemoveAt(0);
                dic.Add(key, String.Join("=", t));
            }
            return dic;
        }
        public static void setCookie(String name, String value, DateTime? expires = null, String path = "/", String domain = null, Boolean secure = false, Boolean httpOnly = false)
        {
            if (expires == null)
                expires = DateTime.Now.AddDays(30);
            try
            {
                HttpCookie myCookie = new HttpCookie(name);
                DateTime now = DateTime.Now;

                // Set the cookie value.
                myCookie.Value = value;
                // Set the cookie expiration date.
                myCookie.Expires = (DateTime)expires;
                myCookie.Domain = domain;
                myCookie.Path = path;
                myCookie.Secure = secure;
                myCookie.HttpOnly = httpOnly;
                // Add the cookie.
                HttpContext.Current.Response.Cookies.Add(myCookie);
            }
            catch (Exception e)
            {//headers probably allready sent}
                String dfhdf = e.Message;
            }
        }
        public static void HttpUploadFile(HttpWebRequest wr, string file, string paramName, string contentType, Dictionary<String, String> nvc)
        {
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            //HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            wr.KeepAlive = true;
            wr.Credentials = System.Net.CredentialCache.DefaultCredentials;

            Stream rs = wr.GetRequestStream();

            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            foreach (string key in nvc.Keys)
            {
                rs.Write(boundarybytes, 0, boundarybytes.Length);
                string formitem = string.Format(formdataTemplate, key, nvc[key]);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                rs.Write(formitembytes, 0, formitembytes.Length);
            }
            rs.Write(boundarybytes, 0, boundarybytes.Length);

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            string header = string.Format(headerTemplate, paramName, file, contentType);
            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
            try
            {
                rs.Write(headerbytes, 0, headerbytes.Length);

                FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                byte[] buffer = new byte[4096];
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    rs.Write(buffer, 0, bytesRead);
                }
                fileStream.Close();

                byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                rs.Write(trailer, 0, trailer.Length);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                rs.Close();
            }
            /*
            WebResponse wresp = null;
            try
            {
                wresp = wr.GetResponse();
                Stream stream2 = wresp.GetResponseStream();
                StreamReader reader2 = new StreamReader(stream2);
            }
            catch (Exception ex)
            {
                if (wresp != null)
                {
                    return wresp;
                    //wresp.Close();
                    //wresp = null;
                }
            }

            return wresp;*/
        }
        public static String getHttpHost(bool trustForwarded = false)
        {
            if (HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_HOST"] != null && trustForwarded)
            {
                return HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_HOST"].ToString();
            }
            return HttpContext.Current.Request.Url.Host;
            /*
                   // use port if non default
                   String port =
                       ((protocol.Equals("http://") && MyUrl.Port != 80) ||
                       (protocol.Equals("https://") && MyUrl.Port != 443))
                       ? ":" + MyUrl.Port : "";

                   // rebuild
                   if (String.IsNullOrWhiteSpace(this.config.appDomain))
                       return protocol + MyUrl.Host + port + MyUrl.LocalPath + query;
                   else
                       return protocol + this.config.appDomain + port + MyUrl.LocalPath + query;
            */
        }
        public static String getHttpProtocol(bool trustForwarded = false)
        {

            //Uri myUrl = HttpContext.Current.Request.Url;
            //return myUrl.Scheme;

            if (HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_PROTO"] != null && trustForwarded)
            {
                if (HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_PROTO"] == "https")
                {
                    return "https";
                }
                return "http";
            }
            /*apache + variants specific way of checking for https*/
            if (HttpContext.Current.Request.ServerVariables["HTTPS"] != null && (HttpContext.Current.Request.ServerVariables["HTTPS"] == "on" || HttpContext.Current.Request.ServerVariables["HTTPS"] == "1"))
            {
                return "https";
            }
            /*nginx way of checking for https*/
            if (HttpContext.Current.Request.ServerVariables["SERVER_PORT"] != null && (HttpContext.Current.Request.ServerVariables["SERVER_PORT"] == "443"))
            {
                return "https";
            }
            return "http";
        }
        
    }
}
