using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace FederationGateway.Core.SessionManagers
{
    public class DefaultSignInSessionManager : ISignInSessionManager
    {
        private readonly string _cookieName;
        private readonly HttpContext _context;

        public DefaultSignInSessionManager(IHttpContextAccessor contextAccessor, string cookieName)
        {
            _context = contextAccessor.HttpContext;
            _cookieName = cookieName;
        }

        public void AddRealm(string realm)
        {
            var realms = ReadCookie();
            if (!realms.Contains(realm))
            {
                realms.Add(realm);
                WriteCookie(realms);
            }
        }

        public IEnumerable<string> GetRealms()
        {
            return ReadCookie();
        }

        public void ClearEndpoints()
        {
            var cookie = _context.Request.Cookies[_cookieName];
            if (cookie != null)
            {
                _context.Response.Cookies.Delete(_cookieName);
            }
        }

        private List<string> ReadCookie()
        {
            var cookie = _context.Request.Cookies[_cookieName];
            if (cookie == null)
            {
                return new List<string>();
            }

            return cookie.Split('|').ToList();
        }

        private void WriteCookie(List<string> realms)
        {
            if (realms.Count == 0)
            {
                ClearEndpoints();
                return;
            }

            var realmString = string.Join("|", realms);

            _context.Response.Cookies.Append(_cookieName, realmString, new CookieOptions
            {
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.None
            });
        }
    }
}
