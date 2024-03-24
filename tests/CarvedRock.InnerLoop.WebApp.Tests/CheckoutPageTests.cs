using AngleSharp;
using AngleSharp.Html.Dom;
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
public class CheckoutPageTests(CustomWebAppFactory factory, ITestOutputHelper outputHelper)
    : IClassFixture<CustomWebAppFactory>
{
    [Fact]
    public async Task GetCheckoutPageAsAnonymousRedirectsToSignIn()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,            
        });

        var pageResponse = await client.GetAsync("/checkout");
        
        Assert.Equal(HttpStatusCode.Redirect, pageResponse.StatusCode);

        var authority = pageResponse.Headers.Location!.Authority;
        Assert.Equal("demo.duendesoftware.com", authority);        
    }

    [Fact]
    public async Task GetLoggedInEmtpyCheckoutPage()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add("X-Authorization", "Erik Smith");
        client.DefaultRequestHeaders.Add("X-Test-idp", "Google");
        client.DefaultRequestHeaders.Add("X-Test-email", "erik@test.com");

        var pageResponse = await client.GetAsync("/checkout");

        // NOTE: might want this to redirect to an empty cart page
        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);        
    }

    [Fact]
    public async Task GetLoggedInCheckoutPageWithItems()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,            
        });
        client.DefaultRequestHeaders.Add("X-Authorization", "Erik Smith");
        client.DefaultRequestHeaders.Add("X-Test-idp", "Google");
        client.DefaultRequestHeaders.Add("X-Test-email", "erik@test.com");

        (var cookieHeader, var expectedGrandTotal) = GetCookieHeaderValueAndGrandTotal();
        client.DefaultRequestHeaders.Add(HeaderNames.Cookie, cookieHeader);

        var pageResponse = await client.GetAsync("/cart");
        var page = await HtmlHelpers.GetDocumentAsync(pageResponse);

        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
        Assert.Equal($"{expectedGrandTotal:C}", page.QuerySelector("[id=grand-total]")!.TextContent);

        outputHelper.WriteLine(page.Body!.OuterHtml);
    }

    [Fact]
    public async Task PostLoggedInCheckoutPageWithItems()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add("X-Authorization", "Erik Smith");
        client.DefaultRequestHeaders.Add("X-Test-idp", "Google");
        client.DefaultRequestHeaders.Add("X-Test-email", "erik@test.com");

        (var cookieHeader, var expectedGrandTotal) = GetCookieHeaderValueAndGrandTotal();
        client.DefaultRequestHeaders.Add(HeaderNames.Cookie, cookieHeader);

        var pageResponse = await client.GetAsync("/checkout");
        var page = await HtmlHelpers.GetDocumentAsync(pageResponse);
        
        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);

        var checkoutResponse = await client.SendAsync(
            (IHtmlFormElement)page.QuerySelector("form[id='checkout']")!,
            (IHtmlInputElement)page.QuerySelector("input[id='submitCheckout']")!);

        // Verify that the response is a redirect to the ThankYou page
        Assert.Equal(HttpStatusCode.Redirect, checkoutResponse.StatusCode);
        Assert.Equal("/ThankYou", checkoutResponse.Headers.Location!.OriginalString);
        
        // Verify that the email was sent and that it looks good
        using (var emailClient = new HttpClient
        { BaseAddress = new Uri(factory.SharedFixture.EmailServerUrl) })
        {
            var emailList = await emailClient.GetFromJsonAsync<IEnumerable<EmailModel>>("/api/messages");
            var email = emailList!.FirstOrDefault(e => e.To == "erik@test.com");
            Assert.NotNull(email);
            Assert.Contains("Your CarvedRock Order", email!.Subject);
            Assert.Equal("e-commerce@carvedrock.com", email.From);

            var emailHtmlResponse = await emailClient.GetStringAsync($"/api/messages/{email.Id}/html");
            var config = Configuration.Default;
            using var context = BrowsingContext.New(config);
            using var doc = await context.OpenAsync(req => req.Content(emailHtmlResponse));
            outputHelper.WriteLine(doc.Body!.InnerHtml);

            var message = doc.QuerySelector("h1")!.TextContent;
            Assert.Contains("Thank you for your order!", message);
        }
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

    public record EmailModel(string To, string Subject, string Body, string From, string Id);
}
