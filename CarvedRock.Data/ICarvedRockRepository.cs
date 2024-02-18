using CarvedRock.Data.Entities;

namespace CarvedRock.Data;

public interface ICarvedRockRepository
{
    Task<List<Product>> GetProductsAsync(string category);
    Task<Product?> GetProductByIdAsync(int id);        
    Task<bool> IsProductNameUniqueAsync(string name);
    Task<Product> CreateProductAsync(Product product);
}
