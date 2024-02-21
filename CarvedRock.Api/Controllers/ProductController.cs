using CarvedRock.Core;
using CarvedRock.Data.Entities;
using CarvedRock.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CarvedRock.Api.Controllers;

[ApiController]
[Route("[controller]")]
public partial class ProductController(ILogger<ProductController> logger, IProductLogic productLogic,
     NewProductValidator validator, IWebHostEnvironment webHostEnv) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IEnumerable<Product>> Get(string category = "all")
    {
        var env = webHostEnv.EnvironmentName;
        return await productLogic.GetProductsForCategoryAsync(category);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        logger.LogDebug("Getting single product in API for {id}", id);
        var product = await productLogic.GetProductByIdAsync(id);
        if (product != null)
        {
            return Ok(product);
        }
        logger.LogWarning("No product found for ID: {id}", id);
        return NotFound();
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    [SwaggerOperation("Creates a single product.")]
    [ProducesResponseType<ProductModel>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] NewProductModel newProduct)
    {
        var validationResult = await validator.ValidateAsync(newProduct);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.CreateValidationProblemDetails(HttpContext));
        }

        var createdProduct = await productLogic.CreateProductAsync(newProduct);
        var uri = Request.Path.Value + $"/{createdProduct.Id}";
        return Created(uri, createdProduct);
    }
}