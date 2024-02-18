using CarvedRock.Core;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarvedRock.WebApp.Pages;

public partial class ListingModel(IProductService productService) : PageModel
{
    public List<ProductModel> Products { get; set; } = [];
    public string CategoryName { get; set; } = "";        

    public async Task OnGetAsync()
    {
        var cat = Request.Query["cat"].ToString();
        if (string.IsNullOrEmpty(cat))
        {
            throw new Exception("failed");
        }
        
        Products = await productService.GetProductsAsync(cat);
        if (Products.Count != 0)
        {
            CategoryName = Products.First().Category.First().ToString().ToUpper() +
                           Products.First().Category[1..];
        }
    }
}
