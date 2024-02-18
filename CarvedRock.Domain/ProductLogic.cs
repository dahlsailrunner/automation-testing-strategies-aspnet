using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using AutoMapper;
using CarvedRock.Core;
using CarvedRock.Data;
using CarvedRock.Data.Entities;
using Microsoft.Extensions.Logging;

namespace CarvedRock.Domain;

public class ProductLogic(ILogger<ProductLogic> logger, ICarvedRockRepository repo,
    IMapper mapper, NewProductValidator validator) : IProductLogic
{
    public async Task<IEnumerable<Product>> GetProductsForCategoryAsync(string category)
    {               
        logger.LogInformation("Getting products in logic for {category}", category);

        Activity.Current?.AddEvent(new ActivityEvent("Getting products from repository"));
        var results = await repo.GetProductsAsync(category);
        Activity.Current?.AddEvent(new ActivityEvent("Retrieved products from repository"));

        return results;
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await repo.GetProductByIdAsync(id);
    }        

    public async Task<ProductModel> CreateProductAsync(NewProductModel newProduct)
    {       
        var productToCreate = mapper.Map<Product>(newProduct);
        var createdProduct = await repo.CreateProductAsync(productToCreate);
        return mapper.Map<ProductModel>(createdProduct);
    }
}