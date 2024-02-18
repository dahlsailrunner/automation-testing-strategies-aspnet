using CarvedRock.Core;
using CarvedRock.Data.Entities;

namespace CarvedRock.Domain;

public interface IProductLogic 
{
    Task<IEnumerable<Product>> GetProductsForCategoryAsync(string category);
    Task<Product?> GetProductByIdAsync(int id);
    Task<ProductModel> CreateProductAsync(NewProductModel newProduct);
}