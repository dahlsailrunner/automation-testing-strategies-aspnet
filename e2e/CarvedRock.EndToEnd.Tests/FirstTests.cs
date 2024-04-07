using CarvedRock.EndToEnd.Tests.Utilities;
using Microsoft.Playwright;

namespace CarvedRock.EndToEnd.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class Tests : BaseTest
{
    [Test]
    public async Task HomePageHasCorrectContent()
    {
        await Page.GotoAsync(WebUrl);

        await Expect(Page).ToHaveTitleAsync("Carved Rock Fitness");
        await Expect(Page.GetByText("GET A GRIP")).ToBeVisibleAsync();
    }

    [Test]
    public async Task ListingPageAddingItemsToCart()
    {
        await Page.GotoAsync($"{WebUrl}/Listing?cat=boots");

        await CheckCartItemCount(Page, 0);

        await Page
            .GetByRole(AriaRole.Row, new () { NameRegex = new Regex("Trailblazer")})
            .GetByRole(AriaRole.Button, new () { NameString = "Add to Cart" }).ClickAsync();
        
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = "screenshot.png" });
        await CheckCartItemCount(Page, 1);

        await Page
              .GetByRole(AriaRole.Row, new() { NameRegex = new Regex("Trailblazer") })
              .GetByRole(AriaRole.Button, new() { NameString = "Add to Cart" }).ClickAsync();

        await CheckCartItemCount(Page, 2);

        await Page
            .GetByRole(AriaRole.Row, new() { NameRegex = new Regex("Woodsman") })
            .GetByRole(AriaRole.Button, new() { NameString = "Add to Cart" }).ClickAsync();

        await CheckCartItemCount(Page, 3);
    }

    private async Task CheckCartItemCount(IPage page, int expectedCount)
    {
        var locator = page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("Cart") });

        await Expect(page.GetByRole(AriaRole.Link,
            new() { NameRegex = new Regex("Cart") }))
            .ToContainTextAsync($"({expectedCount})");
    }
}
