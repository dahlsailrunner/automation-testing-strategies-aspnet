using CarvedRock.InnerLoop.WebApp.Tests.Utilities;
using System.Net;

namespace CarvedRock.InnerLoop.WebApp.Tests;


[Collection(nameof(InnerLoopCollection))]
public class AccessDeniedPageTests(CustomWebAppFactory factory)
    : IClassFixture<CustomWebAppFactory>
{
    [Fact]
    public async Task GetAccessDeniedPage()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Authorization", "Erik Smith");
        client.DefaultRequestHeaders.Add("X-Test-idp", "Microsoft");

        var pageResponse = await client.GetAsync("/accessdenied");
        var accessDeniedPage = await HtmlHelpers.GetDocumentAsync(pageResponse);

        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);

        var actualHeading = accessDeniedPage.QuerySelectorAll("h1").Select(e => e.TextContent);
        Assert.Equal("Access Denied", actualHeading.First());
    }
}
