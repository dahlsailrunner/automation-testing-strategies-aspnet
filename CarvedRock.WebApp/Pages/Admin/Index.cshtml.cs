using CarvedRock.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarvedRock.WebApp.Pages.Admin;

[Authorize(Roles = "admin")]
public class IndexModel(IProductService productService) : PageModel
{
    public List<ProductModel> Products { get; set; } = [];
    public async Task OnGetAsync()
    {
        Products = await productService.GetProductsAsync();
    }
}
