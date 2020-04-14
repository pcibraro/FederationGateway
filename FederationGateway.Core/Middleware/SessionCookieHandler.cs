using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace FederationGateway.Core.Middleware
{
    public class SessionCookieHandler
    {
        private string _cookieName;

        public SessionCookieHandler(string cookieName)
        {
            _cookieName = cookieName;
        }

        public virtual void AddRealm(HttpContext context, string realm)
        {
            var realms = ReadCookie(context);
            if (!realms.Contains(realm))
            {
                realms.Add(realm);
                WriteCookie(context, realms);
            }
        }

        public virtual IEnumerable<string> GetRealms(HttpContext context)
        {
            return ReadCookie(context);
        }

        public virtual void ClearEndpoints(HttpContext context)
        {
            var cookie = context.Request.Cookies[_cookieName];
            if (cookie != null)
            {
                context.Response.Cookies.Delete(_cookieName);
            }
        }

        private List<string> ReadCookie(HttpContext context)
        {
            var cookie = context.Request.Cookies[_cookieName];
            if (cookie == null)
            {
                return new List<string>();
            }

            return cookie.Split('|').ToList();
        }

        private void WriteCookie(HttpContext context, List<string> realms)
        {
            if (realms.Count == 0)
            {
                ClearEndpoints(context);
                return;
            }

            var realmString = string.Join("|", realms);

            context.Response.Cookies.Append(_cookieName, realmString, new CookieOptions
            {
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.None
            });

            context.Response.Headers.Add("P3P", "CP=\"NID DSP ALL COR\"");
        }
    }
}
