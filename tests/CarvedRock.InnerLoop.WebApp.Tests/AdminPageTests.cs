using CarvedRock.InnerLoop.WebApp.Tests.Utilities;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit.Abstractions;

namespace CarvedRock.InnerLoop.WebApp.Tests;

[Collection(nameof(InnerLoopCollection))]
public class AdminPageTests(CustomWebAppFactory factory, ITestOutputHelper outputHelper)
    : IClassFixture<CustomWebAppFactory>
{
    [Fact]
    public async Task GetAdminPageAsAnonymousRedirectsToSignIn()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,            
        });

        var pageResponse = await client.GetAsync("/admin");
        
        Assert.Equal(HttpStatusCode.Redirect, pageResponse.StatusCode);

        var authority = pageResponse.Headers.Location!.Authority;
        Assert.Equal("demo.duendesoftware.com", authority);        
    }

    [Fact]
    public async Task AdminPageForbidden()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Authorization", "Erik Smith");
        client.DefaultRequestHeaders.Add("X-Test-idp", "Microsoft");

        var pageResponse = await client.GetAsync("/admin");        

        Assert.Equal(HttpStatusCode.Forbidden, pageResponse.StatusCode);        
    }

    [InlineData("Erik Smith", "Google")]
    [InlineData("Bob Smith", "Microsoft")]
    [Theory]
    public async Task AdminPageAllowed(string name, string idp)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Authorization", name);
        client.DefaultRequestHeaders.Add("X-Test-idp", idp);

        var pageResponse = await client.GetAsync("/admin");
        var page = await HtmlHelpers.GetDocumentAsync(pageResponse);

        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
        outputHelper.WriteLine(page.Body!.OuterHtml);

        var productItems = page.QuerySelectorAll("tbody>tr");
        Assert.NotEmpty(productItems);
        Assert.True(productItems.Length >= 10);
    }   
}
