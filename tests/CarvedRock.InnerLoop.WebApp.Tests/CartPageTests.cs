using CarvedRock.InnerLoop.WebApp.Tests.Utilities;
using CarvedRock.WebApp.Pages;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Text.Json;
using System.Web;
using Xunit.Abstractions;

namespace CarvedRock.InnerLoop.WebApp.Tests;

[Collection(nameof(InnerLoopCollection))]
public class CartPageTests(CustomWebAppFactory factory, ITestOutputHelper outputHelper)
    : IClassFixture<CustomWebAppFactory>
{
    [Fact]
    public async Task GetCartPageAsAnonymousRedirectsToSignIn()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,            
        });

        var pageResponse = await client.GetAsync("/cart");
        
        Assert.Equal(HttpStatusCode.Redirect, pageResponse.StatusCode);

        var authority = pageResponse.Headers.Location!.Authority;
        Assert.Equal("demo.duendesoftware.com", authority);        
    }

    [Fact]
    public async Task GetLoggedInEmtpyCartPage()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add("X-Authorization", "Erik Smith");
        client.DefaultRequestHeaders.Add("X-Test-idp", "Google");

        var pageResponse = await client.GetAsync("/cart");
        var page = await HtmlHelpers.GetDocumentAsync(pageResponse);

        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);

        var messageParagraph = page.QuerySelector("p");
        Assert.Equal("Your cart is empty. Go add some items!", messageParagraph!.TextContent);
        outputHelper.WriteLine(page.Body!.OuterHtml);
    }

    [Fact]
    public async Task GetLoggedInCartPageWithItems()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,            
        });
        client.DefaultRequestHeaders.Add("X-Authorization", "Erik Smith");
        client.DefaultRequestHeaders.Add("X-Test-idp", "Google");
        
        (var cookieHeader, var expectedGrandTotal) = GetCookieHeaderValueAndGrandTotal();
        client.DefaultRequestHeaders.Add(HeaderNames.Cookie, cookieHeader);

        var pageResponse = await client.GetAsync("/cart");
        var page = await HtmlHelpers.GetDocumentAsync(pageResponse);

        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
        Assert.Equal($"{expectedGrandTotal:C}", page.QuerySelector("[id=grand-total]")!.TextContent);

        outputHelper.WriteLine(page.Body!.OuterHtml);
    }

    private (string, double) GetCookieHeaderValueAndGrandTotal()
    {
        var cookieName = "carvedrock-cart";

        var numberOfItemsToPick = factory.SharedFixture.Faker.Random.Int(1, 3);
        var productsToAdd = factory.SharedFixture.Faker
            .PickRandom(factory.SharedFixture.OriginalProducts, numberOfItemsToPick);

        var cartItems = new List<CartItem>();
        var expectedTotal = 0.0;
        foreach (var product in productsToAdd)
        {
            var quantityOfItem = factory.SharedFixture.Faker.Random.Int(1, 5);
            expectedTotal += product.Price * quantityOfItem;
            var cartItem = new CartItem(product.Id, quantityOfItem, "","",0,0);
            cartItems.Add(cartItem);
        }
        
        var cookieValue = HttpUtility.UrlEncode(JsonSerializer.Serialize(cartItems));

        return (new CookieHeaderValue(cookieName, cookieValue).ToString(), expectedTotal);
    }
}
