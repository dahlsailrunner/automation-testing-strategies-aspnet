using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarvedRock.WebApp.Pages;

public class LogoutModel : PageModel
{
    public async Task OnGetAsync()
    {
        await HttpContext.SignOutAsync();
        Response.Redirect("/");
    }
}
