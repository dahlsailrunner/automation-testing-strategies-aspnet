using CarvedRock.InnerLoop.WebApp.Tests.Utilities;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit.Abstractions;

namespace CarvedRock.InnerLoop.WebApp.Tests;

public class ErrorPageTests(WebApplicationFactory<Program> factory, 
    ITestOutputHelper outputHelper) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetErrorPage()
    {
        var client = factory.CreateClient();
        var errorPageResponse = await client.GetAsync("/Error");
        var errorPage = await HtmlHelpers.GetDocumentAsync(errorPageResponse);        

        Assert.Equal(HttpStatusCode.OK, errorPageResponse.StatusCode);
               
        var actualHeading = errorPage.QuerySelectorAll("h1").Select(e => e.TextContent);
        Assert.Equal("Error.", actualHeading.First());
        
        outputHelper.WriteLine(errorPage.Body!.OuterHtml);
    }
}