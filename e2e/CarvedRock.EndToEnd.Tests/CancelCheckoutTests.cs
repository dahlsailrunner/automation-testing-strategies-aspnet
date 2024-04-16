using CarvedRock.EndToEnd.Tests.Utilities;
using Microsoft.Playwright;
using System.Reflection;

namespace CarvedRock.EndToEnd.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class CancelCheckoutTests : BaseTest
{
    [Test]
    public async Task LoggedInCheckoutCancellation()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Sign in" }).ClickAsync();
        await Page.GetByPlaceholder("Username").ClickAsync();
        await Page.GetByPlaceholder("Username").FillAsync("alice");
        await Page.GetByPlaceholder("Username").PressAsync("Tab");
        await Page.GetByPlaceholder("Password").FillAsync("alice");
        await Page.GetByPlaceholder("Password").PressAsync("Tab");
        await Page.GetByLabel("Remember My Login").PressAsync("Tab");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync();

        await Expect(Page.GetByText("AliceSmith@email.com Sign Out")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Link, new() { Name = "Footwear" }).ClickAsync();
        await Page.Locator("#add-btn-1").ClickAsync();
        await Page.Locator("#add-btn-3").ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Kayaks" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add to Cart" }).ClickAsync();

        // indirectly verifies the cart count
        await Page.GetByRole(AriaRole.Link, new() { Name = "Cart (3)" }).ClickAsync();

        var executionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = "cart.png" });
        Console.WriteLine($"Screenshot saved to {Path.Combine(executionDirectory!, "cart.png")}");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Cancel Order / Clear Cart" }).ClickAsync();
        await Expect(Page.Locator("#carvedrockcart")).ToContainTextAsync("(0)");
    }

    [SetUp]
    public async Task Setup()
    {
        await Context.Tracing.StartAsync(new()
        {
            Title = TestContext.CurrentContext.Test.ClassName + "." + TestContext.CurrentContext.Test.Name,
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
    }

    [TearDown]
    public async Task TearDown()
    {
        // This will produce e.g.:
        // bin/Debug/net8.0/playwright-traces/LoggedInCheckOutCancellation.zip
        await Context.Tracing.StopAsync(new()
        {
            Path = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                "playwright-traces",
                $"{TestContext.CurrentContext.Test.Name}.zip"
            )
        });
    }
}
