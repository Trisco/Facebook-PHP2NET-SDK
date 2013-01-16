using System;
using System.Security.Cryptography;
using System.Text;

namespace facebook
{
    public static class Encryption
    {
        /**
        * Base64 encoding that doesn't need to be urlencode()ed.
        * Exactly the same as base64_encode except it uses
        *   - instead of +
        *   _ instead of /
        *   No padded =
        *
        * @param string $input base64UrlEncoded string
        * @return string
        */
        public static string Base64_Url_Decode(string input)
        {
            var fixedString = string.Empty;
            var fixedDashString = input.Replace('-', '+');
            var fixedUnderscoreString = fixedDashString.Replace('_', '/');
            if (fixedUnderscoreString.Length % 4 != 0)
            {
                fixedString = String.Format("{0}", fixedUnderscoreString);
                int paddingCount = fixedString.Length % 4;
                while (paddingCount % 4 != 0)
                {
                    fixedString += '=';
                    paddingCount++;
                }
            }
            else
            {
                fixedString = fixedUnderscoreString;
            }
            var inputBytes = Convert.FromBase64String(fixedString);
            return Encoding.UTF8.GetString(inputBytes);
        }
        /**
        * Base64 encoding that doesn't need to be urlencode()ed.
        * Exactly the same as base64_encode except it uses
        *   - instead of +
        *   _ instead of /
        *   No padded =
        *
        * @param string $input base64UrlEncoded string
        * @return bytearray
        */
        public static byte[] Base64UrlDecode(string base64UrlSafeString)
        {
            base64UrlSafeString =
                base64UrlSafeString.PadRight(base64UrlSafeString.Length + (4 - base64UrlSafeString.Length % 4) % 4, '=');
            base64UrlSafeString = base64UrlSafeString.Replace('-', '+').Replace('_', '/');
            byte[] encodedDataAsBytes = Convert.FromBase64String(base64UrlSafeString);
            return encodedDataAsBytes;
            //return System.Text.Encoding.ASCII.GetString(encodedDataAsBytes);
            //return Base64Decode(
            //                        encodedData.Replace("-", "+")
            //                                    .Replace("_", "/")
            //                    );
        }
        
        public static string Base64Decode(string encodedData)
        {
            byte[] encodedDataAsBytes = System.Convert.FromBase64String(encodedData);
            string returnValue = System.Text.Encoding.ASCII.GetString(encodedDataAsBytes);
            return returnValue;
        }
        /**
        * Base64 encoding that doesn't need to be urlencode()ed.
        * Exactly the same as base64_encode except it uses
        *   - instead of +
        *   _ instead of /
        *
        * @param string $input string
        * @return string base64Url encoded string
        */
        public static string base64UrlEncode(string input)
        {
            input = Encryption.Base64Encode(input);
            return input.Replace("=", String.Empty).Replace('+', '-').Replace('/', '_');
        }

        public static string Base64Encode(string toEncode)
        {
            byte[] toEncodeAsBytes = System.Text.Encoding.UTF8.GetBytes(toEncode);
            string returnValue = System.Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        public static string Md5Hash(string input)
        {
            // Create a new instance of the MD5CryptoServiceProvider object.
            MD5 md5Hasher = MD5.Create();
            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();
            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
        public static string addslashes(String dirtyString)
        {
            String result = dirtyString;
            try
            {
                result = System.Text.RegularExpressions.Regex.Replace(dirtyString, @"[\000\010\011\012\015\032\042\047\134\140]", "\\$0");
            }catch{}
            return result;
        }

        public static string stripslashes(String dirtyString)
        {
            String result = Uri.UnescapeDataString(dirtyString).Replace("\"", "");
            try
            {
                result = System.Text.RegularExpressions.Regex.Replace(result, @"(\\)([\000\010\011\012\015\032\042\047\134\140])", "$2");
            }
            catch { }
            return result;
        }

        public static string hash_hmac_sha256(string payload, string secret)
        {
            var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
            StringBuilder resp = new StringBuilder() ;
            //Encoding.UTF8.GetString(hmacsha256.Hash);
            foreach (byte test in hmacsha256.Hash)
            {
                resp.Append(test.ToString("X2"));
            }
            return resp.ToString();
        }
    }
}
