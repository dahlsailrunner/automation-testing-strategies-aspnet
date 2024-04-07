using CarvedRock.EndToEnd.Tests.Utilities;
using Microsoft.Playwright;
using System.Reflection;

namespace CarvedRock.EndToEnd.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class CancelCheckoutTests : BaseTest
{
    [Test]
    public async Task LoggedInCheckOutCancellation()
    {
        await Page.GotoAsync("https://localhost:7224/");
        await Page.GetByRole(AriaRole.Link, new() { NameString = "Sign in" }).ClickAsync();
        await Page.WaitForURLAsync("https://demo.duendesoftware.com/Account/Login?**");
        await Page.GetByPlaceholder("Username").ClickAsync();
        await Page.GetByPlaceholder("Username").FillAsync("alice");
        await Page.GetByPlaceholder("Username").PressAsync("Tab");        
        await Page.GetByPlaceholder("Password").FillAsync("alice");
        await Page.GetByRole(AriaRole.Button, new() { NameString = "Login" }).ClickAsync();
        await Page.WaitForURLAsync("https://localhost:7224/");

        // Check that the user is logged in
        await Expect(Page.GetByText("AliceSmith@email.com Sign Out")).ToBeVisibleAsync();


        await Page.GetByRole(AriaRole.Link, new() { NameString = "Footwear" }).ClickAsync();
        await Page.WaitForURLAsync("https://localhost:7224/Listing?cat=boots");

        await Page.GetByRole(AriaRole.Row, new() { NameRegex = new Regex("Woodsman") })
            .GetByRole(AriaRole.Button, new() { NameString = "Add to Cart" }).ClickAsync();

        await Page.GetByRole(AriaRole.Link, new() { NameString = "Kayaks" }).ClickAsync();
        await Page.WaitForURLAsync("https://localhost:7224/Listing?cat=kayak");

        await Page.GetByRole(AriaRole.Button, new() { NameString = "Add to Cart" }).ClickAsync();

        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = "cart.png" });
        var executionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var screenshotPath = Path.Combine(executionDirectory!, "cart.png");
        Console.WriteLine($"Screenshot saved to {screenshotPath}");

        // indirectly checks that cart has been updated
        await Page.GetByRole(AriaRole.Link, new() { NameString = "Cart (2)" }).ClickAsync();
        await Page.WaitForURLAsync("https://localhost:7224/Cart");

        await Page.GetByRole(AriaRole.Button, new() { NameString = "Cancel Order / Clear Cart" }).ClickAsync();
        await Page.WaitForURLAsync("https://localhost:7224/");

        // Check that the cart is empty
        await Expect(Page.GetByRole(AriaRole.Link, new() { NameString = "Cart (0)" })).ToBeVisibleAsync();
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
