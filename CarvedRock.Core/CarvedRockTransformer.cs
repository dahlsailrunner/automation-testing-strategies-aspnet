using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace CarvedRock.Core;
public class CarvedRockTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        ClaimsIdentity claimsIdentity = new();

        if (principal.Claims.Any(c => 
                (c.Type == "idp" && c.Value == "Google") ||
                (c.Type == "name" && c.Value == "Bob Smith")))
        {
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "admin"));
        }

        principal.AddIdentity(claimsIdentity);
        return Task.FromResult(principal);
    }
}
