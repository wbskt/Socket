using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Wbskt.Server
{
    public static class Auth
    {
        public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
        {
            var key = config["Jwt:Key"]!;
            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                };
            });
        }

        public static string GetName(this IPrincipal principal)
        {
            if (principal.Identity is not ClaimsIdentity claimsPrincipal)
                throw new AuthenticationException("Unable to get csid");

            var claim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type.Equals("name", StringComparison.InvariantCulture));
            if (claim == null)
                throw new AuthenticationException("Unable to get csid");

            return claim.Value;
        }

        public static Guid GetChannelSubscriberId(this IPrincipal principal)
        {
            if (principal.Identity is not ClaimsIdentity claimsPrincipal)
                throw new AuthenticationException("Unable to get csid");

            var claim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type.Equals("csid", StringComparison.InvariantCulture));
            if (claim == null)
                throw new AuthenticationException("Unable to get csid");

            return Guid.Parse(claim.Value);
        }

        public static int GetClientId(this IPrincipal principal)
        {
            if (principal.Identity is not ClaimsIdentity claimsPrincipal)
                throw new AuthenticationException("Unable to get csid");

            var claim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type.Equals("cid", StringComparison.InvariantCulture));
            if (claim == null)
                throw new AuthenticationException("Unable to get csid");

            return int.Parse(claim.Value);
        }

        public static int GetServerId(this IPrincipal principal)
        {
            if (principal.Identity is not ClaimsIdentity claimsPrincipal)
                throw new AuthenticationException("Unable to get csid");

            var claim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type.Equals("sad", StringComparison.InvariantCulture));
            if (claim == null)
                throw new AuthenticationException("Unable to get csid");

            return int.Parse(claim.Value.Split(':').First());
        }

        public static Guid GetTokenId(this IPrincipal principal)
        {
            if (principal.Identity is not ClaimsIdentity claimsPrincipal)
                throw new AuthenticationException("Unable to get csid");

            var claim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type.Equals("tid", StringComparison.InvariantCulture));
            if (claim == null)
                throw new AuthenticationException("Unable to get csid");

            return Guid.Parse(claim.Value);
        }
    }
}
