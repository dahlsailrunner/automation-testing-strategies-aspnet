using Bogus;
using Bogus.Extensions;
using CarvedRock.Data;
using CarvedRock.Data.Entities;

namespace CarvedRock.InnerLoop.Tests.Utilities;

public static class DataCreator
{
    public static void InitializeTestData(this LocalContext context, int productCount)
    {        
        var products = ProductFaker.Generate(productCount);

        // could include any other entities or logic here to customize the data

        context.Products.AddRange(products);
        context.SaveChanges();
    }

    public static readonly Faker<Product> ProductFaker = new Faker<Product>()
        .UseSeed(42)
        .RuleFor(p => p.Name, f => f.Commerce.ProductName().ClampLength(max: 50))
        .RuleFor(p => p.Description, f => f.Commerce.ProductDescription().ClampLength(max: 150))
        .RuleFor(p => p.Category, f => f.PickRandom("boots", "equip", "kayak"))
        .RuleFor(p => p.Price, (f, p) =>
                p.Category == "boots" ? f.Random.Double(50, 300) :
                p.Category == "equip" ? f.Random.Double(20, 150) :
                p.Category == "kayak" ? f.Random.Double(100, 500) : 0)
        .RuleFor(p => p.ImgUrl, f => f.Image.PicsumUrl());
}
