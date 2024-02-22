using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace CarvedRock.WebApp.Pages;

public record CartItem(int Id, int Quantity, string Name, string Category, double Price, double Total);

[ValidateAntiForgeryToken]
public class CartModel(IProductService productService) : PageModel
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public List<CartItem> CartContents { get; set; } = [];
    public double CartTotal => CartContents.Sum(c => c.Total);
    public async Task OnGetAsync()
    {
        var cookie = Request.Cookies["carvedrock-cart"];
        if (string.IsNullOrEmpty(cookie)) return;

        CartContents = JsonSerializer.Deserialize<List<CartItem>>(cookie, _jsonOptions)!;

        var allProducts = await productService.GetProductsAsync();

        CartContents = CartContents.Select(cartItem =>
        {
            var product = allProducts.FirstOrDefault(p => p.Id == cartItem.Id)!;

            return new CartItem(cartItem.Id, cartItem.Quantity, product.Name, 
                product.Category, product.Price, product.Price * cartItem.Quantity);                
        }).ToList();       
    }

    public IActionResult OnPostCheckout()
    {
        return RedirectToPage("/Checkout");
    }

    public IActionResult OnPostCancelOrder()
    {
        Response.Cookies.Delete("carvedrock-cart");
        return RedirectToPage("/Index");
    }
}
