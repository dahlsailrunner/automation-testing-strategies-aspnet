using Bogus;
using CarvedRock.Core;
using CarvedRock.InnerLoop.Tests.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Xunit.Abstractions;

namespace CarvedRock.InnerLoop.Tests;

public class ProductControllerTests(CustomApiFactory factory, 
    ITestOutputHelper outputHelper) : IClassFixture<CustomApiFactory>
{
    [Fact]
    public async Task GetProducts_Success()
    {
        var client = factory.CreateClient();
        var products = await client.GetJsonResultAsync<IEnumerable<ProductModel>>
            ("/product?category=all", HttpStatusCode.OK, outputHelper);
        
        Assert.True(products.Count() >= 6);
    }

    [Fact]
    public async Task GetProductById_Success()
    {
        var client = factory.CreateClient();
        var product = await client.GetJsonResultAsync<ProductModel>
            ("/product/2", HttpStatusCode.OK, outputHelper);

        Assert.Equal(2, product.Id);
        Assert.Equal("Coastliner", product.Name);
        Assert.Equal(49.99, product.Price);
        Assert.Equal("boots", product.Category);
    }

    [Fact]
    public async Task GetProductById_NotFound()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/product/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_TroubleGivesProblemDetail()
    {
        var client = factory.CreateClient();
        var problemDetail = await client.GetJsonResultAsync<ProblemDetails>
            ("/product?category=trouble", HttpStatusCode.InternalServerError, outputHelper);
        
        Assert.NotNull(problemDetail.Title);
        Assert.NotNull(problemDetail.Detail);
        Assert.Contains("traceId", problemDetail.Extensions.Keys);
        Assert.NotNull(problemDetail.Extensions["traceId"]);
        Assert.NotEmpty(problemDetail.Extensions["traceId"]!.ToString()!);
    }

    [Fact]
    public async Task PostProductValidationFailure()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Authorization", "Erik Smith");
        client.DefaultRequestHeaders.Add("X-Test-idp", "Google");

        var newProduct = _newProductFaker.Generate();
        newProduct.Name = ""; // invalid

        var problem = await client.PostForJsonResultAsync<ProblemDetails>
            ("/product", newProduct, HttpStatusCode.BadRequest, outputHelper);

        Assert.NotNull(problem);
        Assert.Equal("One or more validation errors occurred.", problem.Detail);
        Assert.Contains("Name", problem.Extensions.Keys);
        Assert.Contains("Name is required.", problem.Extensions["Name"]!.ToString());
    }

    [Fact]
    public async Task PostProductAnonymousIsUnauthorized()
    {
        var client = factory.CreateClient();
        
        var newProduct = _newProductFaker.Generate();

        var response = await client.PostAsJsonAsync("/product", newProduct);        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Erik Smith", "Microsoft")]
    [InlineData("Alice Smith", null)]
    [InlineData("Bob Dahl", null)]
    [InlineData("Bobby Smith", null)]
    public async Task PostProductForbidden(string name, string? idp)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Authorization", name);
        if (idp != null)
        {
            client.DefaultRequestHeaders.Add("X-Test-idp", idp);
        }
        var newProduct = _newProductFaker.Generate();

        var response = await client.PostAsJsonAsync("/product", newProduct);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("Erik Dahl", "Google")]
    [InlineData("Bob Smith", null)]
    [InlineData("Bob Smith", "Google")]
    [InlineData("Alice Smith", "Google")]
    public async Task PostProductSuccess(string name, string? idp)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Authorization", name);
        if (idp != null)
        {
            client.DefaultRequestHeaders.Add("X-Test-idp", idp);
        }
        var newProduct = _newProductFaker.Generate();

        var response = await client.PostForJsonResultAsync<ProductModel>
            ("/product", newProduct, HttpStatusCode.Created, outputHelper);

        Assert.NotNull(response);
        Assert.Equal(newProduct.Name, response.Name);
    }

    private readonly Faker<NewProductModel> _newProductFaker = new Faker<NewProductModel>()
        .RuleFor(p => p.Name, f => f.Commerce.ProductName())
        .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
        .RuleFor(p => p.Category, f => f.PickRandom("boots", "equip", "kayak"))
        .RuleFor(p => p.Price, (f, p) =>
                p.Category == "boots" ? f.Random.Double(50, 300) :
                p.Category == "equip" ? f.Random.Double(20, 150) :
                p.Category == "kayak" ? f.Random.Double(100, 500) : 0)
        .RuleFor(p => p.ImgUrl, f => f.Image.PicsumUrl());
}

