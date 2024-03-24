using Bogus;
using CarvedRock.Core;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Mvc;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace CarvedRock.InnerLoop.WebApp.Tests.Utilities;

[CollectionDefinition(nameof(InnerLoopCollection))]
public class InnerLoopCollection : ICollectionFixture<SharedFixture>
{
    // This class has no code, and is never created. Its purpose is simply to be the place
    // to apply [CollectionDefinition] and all the ICollectionFixture<> interfaces.
}

public class SharedFixture : IAsyncLifetime
{
    public readonly Faker Faker = new();
    public List<ProductModel> OriginalProducts { get; private set; } = null!;
    
    private static readonly List<string> _categories = ["boots", "equip", "kayak"];

    public readonly Faker<ProductModel> ProductFaker = new Faker<ProductModel>()
        .RuleFor(p => p.Id, f => f.UniqueIndex + 1)
        .RuleFor(p => p.Name, f => f.Commerce.ProductName())
        .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
        .RuleFor(p => p.Price, f => f.Random.Double(10, 1000))
        .RuleFor(p => p.Category, f => f.PickRandom(_categories))
        .RuleFor(p => p.ImgUrl, f => f.Image.PicsumUrl());    

    public async Task InitializeAsync()
    {       
        await _emailContainer.StartAsync();

        OriginalProducts = ProductFaker.Generate(10);

        ProductServiceUrl = StartWireMockForProductService();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    // SMTP4DEV Email Server ---------------------------
    public string EmailServerUrl => $"http://localhost:{_emailContainer.GetMappedPublicPort(80)}";
    public ushort EmailPort => _emailContainer.GetMappedPublicPort(25);

    private readonly IContainer _emailContainer = new ContainerBuilder()
        .WithImage("rnwood/smtp4dev")
        .WithPortBinding(25, assignRandomHostPort: true)
        .WithPortBinding(80, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Now listening on:"))
        .WithCleanUp(true)
        .Build();

    //// WireMock for Product Service --------------------
    public string ProductServiceUrl { get; private set; } = null!;

    private string StartWireMockForProductService()
    {
        var server = WireMockServer.Start();

        // NOTES: Order matters -- make the MORE SPECIFIC routes AFTER the more general ones
        // Also - the "WithPath" method needs to have the forward /
        server
            .Given(Request.Create().WithPath("/Product").WithParam("category").UsingGet())
            .RespondWith(Response.Create().WithBodyAsJson(GetProductsBasedOnCategory));

        server
            .Given(Request.Create().WithPath("/Product").WithParam("category", "error").UsingGet())
            .RespondWith(Response.Create()
            .WithDelay(TimeSpan.FromMilliseconds(Faker.Random.Int(0, 4000))) // delay anywhere from 0 to 4 seconds
            .WithBodyAsJson(GetProblemDetail)
            .WithStatusCode(500));

        return server.Urls[0];
    }

    private object GetProblemDetail(IRequestMessage message)
    {
        return new ProblemDetails
        {
            Detail = "An error occurred while processing your request.",
            Status = 500,
            Title = "Internal Server Error",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
    }

    private object GetProductsBasedOnCategory(IRequestMessage message)
    {
        var category = message.GetParameter("category")!.First();
        return category == "all"
            ? OriginalProducts
            : OriginalProducts.Where(p => p.Category == category).ToList();
    }
}
