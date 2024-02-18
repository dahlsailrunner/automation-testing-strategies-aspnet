using CarvedRock.WebApp.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;

namespace CarvedRock.WebApp.Pages;

[Authorize]
[ValidateAntiForgeryToken]
public class CheckoutModel(IProductService productService, IEmailSender emailService) : PageModel
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string EmailAddress { get; set; } = "";
    
    public List<CartItem> CartContents { get; set; } = [];
    public double CartTotal => CartContents.Sum(c => c.Total);
    public async Task OnGetAsync()
    {
        EmailAddress = User.Claims.First(c=> c.Type == "email").Value;

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

    public async Task<IActionResult> OnPostSubmitOrder()
    {
        EmailAddress = User.Claims.First(c => c.Type == "email").Value;

        var cookie = Request.Cookies["carvedrock-cart"];
        if (string.IsNullOrEmpty(cookie)) return RedirectToPage("/Cart");

        CartContents = JsonSerializer.Deserialize<List<CartItem>>(cookie, _jsonOptions)!;

        var allProducts = await productService.GetProductsAsync();

        CartContents = CartContents.Select(cartItem =>
        {
            var product = allProducts.FirstOrDefault(p => p.Id == cartItem.Id)!;

            return new CartItem(cartItem.Id, cartItem.Quantity, product.Name,
                product.Category, product.Price, product.Price * cartItem.Quantity);
        }).ToList();

        var template = Resources.emailTemplate;

        var emailTemplate = new StringBuilder();
        emailTemplate.AppendLine($"<h1>Thank you for your order!</h1>");
        emailTemplate.AppendLine("<table>");
        emailTemplate.AppendLine("<tr><th>Product</th><th>Quantity</th><th>Price</th></tr>");
        foreach (var cartItem in CartContents)
        {
            emailTemplate.AppendLine($"<tr><td>{cartItem.Name}</td><td>{cartItem.Quantity}</td><td>{cartItem.Total}</td></tr>");
        }
        emailTemplate.AppendLine("</table>");

        template = template.Replace("##CONTENT##", emailTemplate.ToString());

        await emailService.SendEmailAsync(EmailAddress, "Your CarvedRock Order", template);

        Response.Cookies.Delete("carvedrock-cart");
        return RedirectToPage("/ThankYou");
    }

    public IActionResult OnPostCancelOrder()
    {
        Response.Cookies.Delete("carvedrock-cart");
        return RedirectToPage("/Index");
    }
}
