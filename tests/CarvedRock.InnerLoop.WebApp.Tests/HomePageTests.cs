using CarvedRock.InnerLoop.WebApp.Tests.Utilities;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit.Abstractions;

namespace CarvedRock.InnerLoop.WebApp.Tests;

public class HomePageTests(WebApplicationFactory<Program> factory, 
    ITestOutputHelper outputHelper) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly List<string> _alwaysPresentNavItems = ["Footwear", "Kayaks", "Equipment", "Cart", "IdSrv"];

    [Fact]
    public async Task GetHomePage()
    {
        var client = factory.CreateClient();
        var homePageResponse = await client.GetAsync("/");               

        Assert.Equal(HttpStatusCode.OK, homePageResponse.StatusCode);

        //var homepagecontent = await homePageResponse.Content.ReadAsStringAsync();
        //outputHelper.WriteLine(homepagecontent);

        var homePage = await HtmlHelpers.GetDocumentAsync(homePageResponse);

        Assert.NotNull(homePage.Title);
        Assert.Equal("Carved Rock Fitness", homePage.Title);

        var actualNavItems = homePage.QuerySelectorAll(".nav-item").Select(e => e.TextContent);

        var additionalNavItems = new List<string> { "Sign in" };
        var expectedNavItems = _alwaysPresentNavItems.Concat(additionalNavItems);

        Assert.Equal(expectedNavItems.Count(), actualNavItems.Count());
        foreach (var expectedNavItem in expectedNavItems)
        {
            outputHelper.WriteLine($"Checking for nav item: {expectedNavItem}");
            Assert.Contains(actualNavItems, item => item.Contains(expectedNavItem));
        }

        var actualImages = homePage.Images.Select(i => i.Source); // could review images

        outputHelper.WriteLine(homePage.Body!.OuterHtml);
    }

    // TODO: Add a test that an appropriate logged-in user has the admin link
}