using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;

namespace CarvedRock.WebApp.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
[AllowAnonymous]
public class ErrorModel(ILogger<ErrorModel> logger) : PageModel
{
    public string? RequestId { get; set; }
    public Activity? CurrentActivity { get; set; }
    public string? TraceId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);    

    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        CurrentActivity = Activity.Current;
        TraceId = HttpContext.TraceIdentifier;
        
        var userName = User.Identity?.IsAuthenticated ?? false? User.Identity.Name : "";
        logger.LogWarning("User {userName} experienced an error.", userName);
    }
}