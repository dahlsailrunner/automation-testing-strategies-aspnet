using CarvedRock.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CarvedRock.WebApp;

public interface IProductService
{
    Task<List<ProductModel>> GetProductsAsync(string category = "all");
    Task<IDictionary<string, string>> AddProductAsync(NewProductModel newProduct);
}

public class ProductService : IProductService
{
    private readonly ILogger<ProductService> _logger;
    private readonly IHttpContextAccessor _httpCtxAccessor;

    private HttpClient Client { get; }

    public ProductService(HttpClient client, IConfiguration config, ILogger<ProductService> logger, 
        IHttpContextAccessor httpCtxAccessor)
    {
        client.BaseAddress = new Uri(config.GetValue<string>("CarvedRock:ApiBaseUrl")!);
        Client = client;
        _logger = logger;
        _httpCtxAccessor = httpCtxAccessor;
    }

    public async Task<List<ProductModel>> GetProductsAsync(string category = "all")
    {
        var _httpCtx = _httpCtxAccessor.HttpContext;
        if (_httpCtx != null)
        {
            var accessToken = await _httpCtx.GetTokenAsync("access_token");
            Client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", accessToken);
            // for a better way to include and manage access tokens for API calls:
            // https://identitymodel.readthedocs.io/en/latest/aspnetcore/web.html
        }

        var response = await Client.GetAsync($"Product?category={category}");
        if (!response.IsSuccessStatusCode)
        {
            var fullPath = $"{Client.BaseAddress}Product?category={category}";
           
            var content = await response.Content.ReadAsStringAsync();
            var details = JsonSerializer.Deserialize<ProblemDetails>(content) ?? new ProblemDetails();
            var traceId = details.Extensions["traceId"]?.ToString();

            _logger.LogWarning("API failure: {fullPath} Response: {apiResponse}, Trace: {trace}",
                fullPath, (int)response.StatusCode, traceId);

            throw new Exception("API call failed!");
        }

        return await response.Content.ReadFromJsonAsync<List<ProductModel>>() ?? [];
    }

    public async Task<IDictionary<string, string>> AddProductAsync(NewProductModel newProduct)
    {
        var _httpCtx = _httpCtxAccessor.HttpContext;
        if (_httpCtx != null)
        {
            var accessToken = await _httpCtx.GetTokenAsync("access_token");
            Client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", accessToken);
            // for a better way to include and manage access tokens for API calls:
            // https://identitymodel.readthedocs.io/en/latest/aspnetcore/web.html
        }
        var response = await Client.PostAsJsonAsync("Product", newProduct);

        if (!response.IsSuccessStatusCode)
        {            
            var content = await response.Content.ReadAsStringAsync();
            var details = JsonSerializer.Deserialize<ProblemDetails>(content) ?? new ProblemDetails();

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var validationErrors = new Dictionary<string, string>();
                var ignore = new List<string> { "traceId", "type", "title", "status", "detail", "instance" };
                foreach (var errorKey in details.Extensions.Keys.Where(k => !ignore.Contains(k)))
                {
                    var errorMessages= details.Extensions[errorKey]!.ToString() ?? "";
                    validationErrors.Add(errorKey, errorMessages);
                }
                return validationErrors;
            }

            var traceId = details.Extensions["traceId"]?.ToString();

            var fullPath = $"{Client.BaseAddress}Product";
            _logger.LogWarning("API failure: {fullPath} Response: {apiResponse}, Trace: {trace}",
                fullPath, (int)response.StatusCode, traceId);

            throw new Exception("API call failed!");
        }

        return new Dictionary<string, string>();
    }
}
