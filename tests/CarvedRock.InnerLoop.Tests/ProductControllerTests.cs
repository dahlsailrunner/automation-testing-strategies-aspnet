using Bogus;
using CarvedRock.Core;
using CarvedRock.InnerLoop.Tests.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Xunit.Abstractions;

namespace CarvedRock.InnerLoop.Tests;

[Collection(nameof(InnerLoopCollection))]
public class ProductControllerTests(CustomApiFactory factory, 
    ITestOutputHelper outputHelper) : BaseTest(factory), IClassFixture<CustomApiFactory>
{
    private readonly Faker _faker = new();

    [Fact]
    public async Task GetProducts_Success()
    {
        var products = await Client.GetJsonResultAsync<IEnumerable<ProductModel>>
            ("/product?category=all", HttpStatusCode.OK, outputHelper);
        
        Assert.True(products.Count() >= SharedContext.OriginalProducts!.Count);  
        
        foreach (var expectedProduct in SharedContext.OriginalProducts!)
        {
            Assert.Contains(products, p => p.Id == expectedProduct.Id);
        }
    }

    [Fact]
    public async Task GetProductById_Success()
    {
        var expectedProduct = _faker.PickRandom(SharedContext.OriginalProducts!);

        var product = await Client.GetJsonResultAsync<ProductModel>
            ($"/product/{expectedProduct.Id}", HttpStatusCode.OK, outputHelper);

        Assert.Equal(expectedProduct.Id, product.Id);
        Assert.Equal(expectedProduct.Name, product.Name);
        Assert.Equal(expectedProduct.Description, product.Description);
        Assert.Equal(expectedProduct.Category, product.Category);
        Assert.Equal(expectedProduct.Price, product.Price);
    }

    [Fact]
    public async Task GetProductById_NotFound()
    {
        var response = await Client.GetAsync("/product/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_TroubleGivesProblemDetail()
    {
        var problemDetail = await Client.GetJsonResultAsync<ProblemDetails>
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
        Client.DefaultRequestHeaders.Add("X-Authorization", "Erik Smith");
        Client.DefaultRequestHeaders.Add("X-Test-idp", "Google");

        var newProduct = SharedContext.NewProductFaker.Generate();
        newProduct.Name = ""; // invalid

        var problem = await Client.PostForJsonResultAsync<ProblemDetails>
            ("/product", newProduct, HttpStatusCode.BadRequest, outputHelper);

        Assert.NotNull(problem);
        Assert.Equal("One or more validation errors occurred.", problem.Detail);
        Assert.Contains("Name", problem.Extensions.Keys);
        Assert.Contains("Name is required.", problem.Extensions["Name"]!.ToString());
    }

    [Fact]
    public async Task PostProductAnonymousIsUnauthorized()
    {
        var newProduct = SharedContext.NewProductFaker.Generate();

        var response = await Client.PostAsJsonAsync("/product", newProduct);        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Erik Smith", "Microsoft")]
    [InlineData("Alice Smith", null)]
    [InlineData("Bob Dahl", null)]
    [InlineData("Bobby Smith", null)]
    public async Task PostProductForbidden(string name, string? idp)
    {
        Client.DefaultRequestHeaders.Add("X-Authorization", name);
        if (idp != null)
        {
            Client.DefaultRequestHeaders.Add("X-Test-idp", idp);
        }
        var newProduct = SharedContext.NewProductFaker.Generate();

        var response = await Client.PostAsJsonAsync("/product", newProduct);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("Erik Dahl", "Google")]
    [InlineData("Bob Smith", null)]
    [InlineData("Bob Smith", "Google")]
    [InlineData("Alice Smith", "Google")]
    public async Task PostProductSuccess(string name, string? idp)
    {
        Client.DefaultRequestHeaders.Add("X-Authorization", name);
        if (idp != null)
        {
            Client.DefaultRequestHeaders.Add("X-Test-idp", idp);
        }
        var newProduct = SharedContext.NewProductFaker.Generate();

        var response = await Client.PostForJsonResultAsync<ProductModel>
            ("/product", newProduct, HttpStatusCode.Created, outputHelper);

        Assert.NotNull(response);
        Assert.Equal(newProduct.Name, response.Name);
    }

    
}

