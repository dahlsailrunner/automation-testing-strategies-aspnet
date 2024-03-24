using CarvedRock.WebApp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NSubstitute;

namespace CarvedRock.InnerLoop.WebApp.Tests.Utilities;

public class CustomWebAppFactory(SharedFixture fixture) : WebApplicationFactory<Program>
{
    public SharedFixture SharedFixture => fixture;
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("innerloop-test");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["CarvedRock:EmailPort"] = SharedFixture.EmailPort.ToString(),
                ["CarvedRock:ApiBaseUrl"] = SharedFixture.ProductServiceUrl
            }!);
        });

        //builder.ConfigureServices(ProvideSubstituteForProductService);

        builder.ConfigureTestServices(services => services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { }));        
    }        
    
    //private void ProvideSubstituteForProductService(IServiceCollection services)
    //{
    //    var productService = services.SingleOrDefault(
    //                   d => d.ServiceType == typeof(IProductService));
    //    services.Remove(productService!);            

    //    var productServiceMock = Substitute.For<IProductService>();
        
    //    productServiceMock
    //        .GetProductsAsync(Arg.Any<string>())
    //        .Returns(args => SharedFixture.OriginalProducts
    //                               .Where(p => p.Category == (string)args[0]).ToList());

    //    productServiceMock
    //        .GetProductsAsync()
    //        .Returns(args => SharedFixture.OriginalProducts);

    //    services.AddScoped(_ => productServiceMock);            
    //}
}
