using System.Security.Claims;
using System.Threading.Tasks;

namespace FederationGateway.Providers.Profiles
{
    public interface IProfileManager
    {
        Task<ClaimsIdentity> GetProfileAsync(SignInRequest request);
    }
}
