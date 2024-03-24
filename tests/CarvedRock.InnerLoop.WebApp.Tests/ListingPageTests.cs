using CarvedRock.InnerLoop.WebApp.Tests.Utilities;
using System.Net;
using Xunit.Abstractions;

namespace CarvedRock.InnerLoop.WebApp.Tests;

[Collection(nameof(InnerLoopCollection))]
public class ListingPageTests(CustomWebAppFactory factory, ITestOutputHelper outputHelper) 
    : IClassFixture<CustomWebAppFactory>
{
    [InlineData("boots")]
    [InlineData("kayak")]
    [InlineData("equip")]
    [InlineData("nothing")]
    [Theory]
    public async Task GetListingPage(string category)
    {
        var client = factory.CreateClient();
        var pageResponse = await client.GetAsync($"/listing?cat={category}");
        var page = await HtmlHelpers.GetDocumentAsync(pageResponse);

        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);

        outputHelper.WriteLine(page.Body!.OuterHtml);

        var productNameCells = page.QuerySelectorAll("tr>td:first-child");
        var productNames = productNameCells.Select(c => c.TextContent.Trim());
        var buttons = page.QuerySelectorAll("tr>td>button");   

        Assert.Equal(factory.SharedFixture.OriginalProducts.Count(p => p.Category == category), 
            productNames.Count());

        foreach (var expectedProduct in factory.SharedFixture.OriginalProducts
                    .Where(p => p.Category == category))
        {
            Assert.Contains(expectedProduct.Name, productNames);            
        }
        
        foreach (var button in buttons)
        {
            Assert.Contains("Add to Cart", button.TextContent);            
        }
    }

    [Fact]
    public async Task GetListingPageWithError()
    {
        var client = factory.CreateClient();
        var pageResponse = await client.GetAsync($"/listing?cat=error");
        var page = await HtmlHelpers.GetDocumentAsync(pageResponse);

        Assert.Equal(HttpStatusCode.InternalServerError, pageResponse.StatusCode);
        outputHelper.WriteLine(page.Body!.OuterHtml);

        var actualHeading = page.QuerySelectorAll("h1").Select(e => e.TextContent);
        Assert.Equal("Error.", actualHeading.First());
        var actualMessage = page.QuerySelectorAll("h2").Select(e => e.TextContent);
        Assert.StartsWith("An error occurred", actualMessage.First());
    }
}
