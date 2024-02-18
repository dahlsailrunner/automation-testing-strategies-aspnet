using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarvedRock.WebApp.Pages;

public class LoginModel : PageModel
{
    public async Task OnGetAsync()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Request.Query["ReturnUrl"].FirstOrDefault() ?? "/"                
        };
        await HttpContext.ChallengeAsync(properties);
    }
}
