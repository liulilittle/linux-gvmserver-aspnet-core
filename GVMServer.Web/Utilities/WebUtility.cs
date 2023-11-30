namespace GVMServer.Web.Utilities
{
    using System;
    using System.ComponentModel;
    using System.Net;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Web;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;

    public static class WebUtility
    {
        public static string RewriteQueryString(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            else
            {
                s = Regex.Replace(s, "%3D", "=", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                s = Regex.Replace(s, "%26", "&", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            }
            string results = string.Empty;
            int i = -1;
            while ((i = s.IndexOf('=')) >= 0)
            {
                string key = s.Substring(0, i);
                s = s.Substring(++i);
                string value = s;
                int j = value.IndexOf('&');
                if (j >= 0)
                {
                    s = s.Substring(j + 1);
                    value = HttpUtility.UrlEncode(value.Substring(0, j));
                }
                else
                {
                    value = HttpUtility.UrlEncode(value);
                }
                results += $"{(results.Length > 0 ? "&" : string.Empty)}{key}={value}";
                if (j < 0)
                {
                    break;
                }
            }
            return results;
        }

        public static string GetQueryValue(this HttpContext context, string key)
        {
            if (context == null || string.IsNullOrEmpty(key))
            {
                return null;
            }
            var request = context.Request;
            if (request == null)
            {
                return null;
            }
            string s = request.Query[key];
            if (string.IsNullOrEmpty(s))
            {
                string method = request.Method ?? string.Empty;
                method = method.Trim().ToUpper();
                if (method == "POST")
                {
                    s = request.Form[key];
                }
            }
            if (!string.IsNullOrEmpty(s))
            {
                s = HttpUtility.UrlDecode(s);
            }
            return s;
        }

        /// <summary>
        /// New Guid String
        /// </summary>
        /// <param name="format">
        ///    A single format specifier that indicates how to format the value of this System.Guid.
        //     The format parameter can be "N", "D", "B", "P", or "X". If format is null or
        //     an empty string (""), "D" is used.
        // </param>
        /// <returns></returns>
        public static string NewGuidString(string format = "D")
        {
            return Guid.NewGuid().ToString(format);
        }

        public static int NewGuidHash32()
        {
            int hash = 0;
            while (hash == 0)
            {
                hash = NewGuidString().GetHashCode();
            }
            return hash;
        }

        public static string GetDescription(this Enum e)
        {
            Type type = e.GetType();
            if (type == null)
            {
                return null;
            }

            string s = Enum.GetName(type, e);
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            var fi = type.GetField(s, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi == null)
            {
                return null;
            }

            DescriptionAttribute attribute = fi.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute;
            if (attribute == null)
            {
                return null;
            }

            return attribute.Description;
        }

        public static IPAddress GetRemoteIpAddress(this HttpContext context)
        {
            IConfigurationSection config = Startup.GetDefaultConfiguration().GetSection("Web");
            IPAddress address = null;
            if (config.GetValue<bool>("GetIPAddressFromHttpHeaders"))
            {
                //proxy_set_header Host $host;
                //proxy_set_header X-Real - IP $remote_addr;
                //proxy_set_header REMOTE-HOST $remote_addr;
                //proxy_set_header X-Forwarded - For $proxy_add_x_forwarded_for;

                string remote_addr = context.Request.Headers["X-Real-IP"];
                if (string.IsNullOrEmpty(remote_addr))
                {
                    remote_addr = context.Request.Headers["REMOTE-HOST"];
                }
                if (string.IsNullOrEmpty(remote_addr))
                {
                    remote_addr = context.Request.Headers["X-Forwarded-For"];
                }
                if (!string.IsNullOrEmpty(remote_addr))
                {
                    IPAddress.TryParse(remote_addr, out address);
                }
                if (address == null)
                {
                    address = IPAddress.Any;
                }
            }
            else
            {
                address = context.Connection.RemoteIpAddress;
            }
            return address;
        }
    }
}
