using CarvedRock.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarvedRock.Data;

public class CarvedRockRepository(LocalContext ctx, ILogger<CarvedRockRepository> logger) : ICarvedRockRepository
{
    public async Task<List<Product>> GetProductsAsync(string category)
    {          
        logger.LogInformation("Getting products in repository for {category}", category);

        List<string> validCategories = ["kayak", "equip", "boots", "all"];

        try
        {
            if (!validCategories.Contains(category))
            {
                throw new Exception($"Simulated exception for category {category}");
            }
            return await ctx.Products.Where(p => p.Category == category || category == "all").ToListAsync();
        } 
        catch (Exception ex)
        {
            var newEx = new ApplicationException("Something bad happened in database", ex);
            newEx.Data.Add("Category", category);
            throw newEx;
        }
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await ctx.Products.FindAsync(id);
    }

    public Task<bool> IsProductNameUniqueAsync(string name)
    {
        return ctx.Products.AllAsync(p => p.Name != name);
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        product.Name = product.Name!.Trim();
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();
        return product;
    }
}
