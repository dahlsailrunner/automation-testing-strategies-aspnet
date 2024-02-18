using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarvedRock.WebApp.Pages;

[Authorize]
public class ThankYouModel : PageModel
{
    public void OnGet()
    {
    }
}
