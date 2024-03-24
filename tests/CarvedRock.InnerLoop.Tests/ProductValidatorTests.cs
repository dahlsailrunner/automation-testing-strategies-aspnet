using Bogus;
using CarvedRock.Core;
using CarvedRock.Data;
using CarvedRock.Domain;
using NSubstitute;
using Xunit.Abstractions;

namespace CarvedRock.InnerLoop.Tests;

public class ProductValidatorTests(ITestOutputHelper outputHelper)
{
    private readonly Faker _faker = new();

    [Theory]
    [InlineData("__max_name__", "Test Description", "boots", 50, "https://test.com/test.jpg")]
    [InlineData("TestName", "__max_description__", "boots", 300, "https://test.com/test.jpg")]
    [InlineData("__max_name__", "Test Description", "kayak", 100, "https://test.com/test.jpg")]
    [InlineData("TestName", "__max_description__", "kayak", 500, "https://test.com/test.jpg")]
    [InlineData("__max_name__", "Test Description", "equip", 20, "https://test.com/test.jpg")]
    [InlineData("TestName", "__max_description__", "equip", 150, "https://test.com/test.jpg")]
    public async Task SuccessfulValidation(string name, string description, string category, 
        double price, string imgUrl)
    {
        // arrange --------------------------------------------------------
        var repo = Substitute.For<ICarvedRockRepository>();
        repo.IsProductNameUniqueAsync(Arg.Any<string>()).Returns(true);

        NewProductValidator validator = new(repo);
        var modelToValidate = new NewProductModel
        {
            Name = name == "__max_name__" 
                ? _faker.Lorem.Letter(50) 
                : name,
            Description = description == "__max_description__" 
                ? _faker.Lorem.Letter(150) 
                : description,
            Category = category,
            Price = price,
            ImgUrl = imgUrl
        };

        // act --------------------------------------------------------
        var result = await validator.ValidateAsync(modelToValidate);

        // assert --------------------------------------------------------
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Name is required.")]
    [InlineData(" ", "Name is required.")]
    [InlineData("duplicate", "A product with the same name already exists.")]
    [InlineData("__too_long__", "Name must not exceed 50 characters.")]
    public async Task NameValidationErrors(string nameToValidate, string errorMessage)
    { 
        //arrange --------------------
        var newProduct = new NewProductModel
        {
            Name = nameToValidate == "__too_long__"? _faker.Lorem.Letter(51) : nameToValidate,
            Description = "A new product",
            Category = "boots",
            Price = 100,
            ImgUrl = "http://www.example.com/image.jpg"
        };
        var repo = Substitute.For<ICarvedRockRepository>();
        repo.IsProductNameUniqueAsync(Arg.Any<string>()).Returns(true);
        repo.IsProductNameUniqueAsync("duplicate").Returns(false);

        var validator = new NewProductValidator(repo);

        //act ------------------------
        var result = await validator.ValidateAsync(newProduct);
        outputHelper.WriteLine(result.ToString());

        //assert ---------------------
        Assert.False(result.IsValid);
        Assert.Equal(errorMessage, result.Errors[0].ErrorMessage);
    }

    [Fact]    
    public async Task CategoryValiationErrors()
    {
        //arrange --------------------
        var newProduct = new NewProductModel
        {
            Name = "product-name",
            Description = "A new product",
            Category = "not a category",
            Price = 100,
            ImgUrl = "http://www.example.com/image.jpg"
        };
        var repo = Substitute.For<ICarvedRockRepository>();
        repo.IsProductNameUniqueAsync(Arg.Any<string>()).Returns(true);

        var validator = new NewProductValidator(repo);

        //act ------------------------
        var result = await validator.ValidateAsync(newProduct);
        outputHelper.WriteLine(result.ToString());

        //assert ---------------------
        Assert.False(result.IsValid);
        Assert.StartsWith("Category must be one of ", result.Errors[0].ErrorMessage);
    }

    [Theory]
    [InlineData("", "Description is required.")]
    [InlineData(" ", "Description is required.")]
    [InlineData("__too_long__", "Description must not exceed 150 characters.")]
    public async Task DescriptionValidationErrors(string descriptionToUse, string errorMessage)
    {
        // arrange --------------------------------------------------------
        var repo = Substitute.For<ICarvedRockRepository>();
        repo.IsProductNameUniqueAsync(Arg.Any<string>()).Returns(true);

        NewProductValidator validator = new(repo);
        var modelToValidate = new NewProductModel
        {
            Name = "some name",
            Description = descriptionToUse == "__too_long__" 
                    ? _faker.Lorem.Letter(151) 
                    : descriptionToUse,
            Category = "boots",
            Price = 100,
            ImgUrl = "https://test.com/test.jpg"
        };

        // act --------------------------------------------------------
        var result = await validator.ValidateAsync(modelToValidate);
        outputHelper.WriteLine(result.ToString());

        // assert --------------------------------------------------------
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(errorMessage, result.Errors[0].ErrorMessage);
    }

    [Theory]
    [InlineData("", "Img Url must be a valid URL.")]
    [InlineData("not-an-url", "Img Url must be a valid URL.")]
    [InlineData("__too_long__", "Img Url must not exceed 255 characters.")]
    public async Task ImgUrlValidationErrors(string imgUrlToUse, string errorMessage)
    {
        // arrange --------------------------------------------------------
        var repo = Substitute.For<ICarvedRockRepository>();
        repo.IsProductNameUniqueAsync(Arg.Any<string>()).Returns(true);

        NewProductValidator validator = new(repo);
        var modelToValidate = new NewProductModel
        {
            Name = "some name",
            Description = "some description",
            Category = "boots",
            Price = 100,
            ImgUrl = imgUrlToUse == "__too_long__" 
                ? $"https://www.someplace.com/{_faker.Lorem.Letter(250)}" 
                : imgUrlToUse,
        };

        // act --------------------------------------------------------
        var result = await validator.ValidateAsync(modelToValidate);

        // assert --------------------------------------------------------
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(errorMessage, result.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task MultipleValidationErrors()
    {
        // arrange --------------------------------------------------------
        var repo = Substitute.For<ICarvedRockRepository>();
        repo.IsProductNameUniqueAsync("DuplicateProductName").Returns(false);
        NewProductValidator validator = new(repo);
        var modelToValidate = new NewProductModel
        {
            Name = "DuplicateProductName",  // duplicate
            Description = _faker.Lorem.Letter(155), // too long
            Category = "boots", // ok
            Price = 0.01, // below the minimum
            ImgUrl = "invalid-url", // invalid url
        };

        // act --------------------------------------------------------
        var result = await validator.ValidateAsync(modelToValidate);

        // assert --------------------------------------------------------
        Assert.False(result.IsValid);
        Assert.Equal(4, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "A product with the same name already exists.");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Description must not exceed 150 characters.");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Price for boots must be between $50.00 and $300.00");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Img Url must be a valid URL.");
    }

    [Theory]
    //[InlineData("boots", 49.99, "Price for boots must be between $50.00 and $300.00")]
    //[InlineData("boots", 300.01, "Price for boots must be between $50.00 and $300.00")]
    //[InlineData("equip", 19.99, "Price for equip must be between $20.00 and $150.00")]
    //[InlineData("equip", 150.01, "Price for equip must be between $20.00 and $150.00")]
    //[InlineData("kayak", 99.99, "Price for kayak must be between $100.00 and $500.00")]
    //[InlineData("kayak", 500.01, "Price for kayak must be between $100.00 and $500.00")]
    [MemberData(nameof(GetNewPriceValidationFailureData))]
    public async Task PriceValidationErrors(string category, double price, string errorMessage)
    {
        // arrange --------------------------------------------------------
        var repo = Substitute.For<ICarvedRockRepository>();
        repo.IsProductNameUniqueAsync(Arg.Any<string>()).Returns(true);

        NewProductValidator validator = new(repo);
        var modelToValidate = new NewProductModel
        {
            Name = "some name",
            Description = "some description",
            Category = category,
            Price = price,
            ImgUrl = "https://test.com/test.jpg",
        };

        // act --------------------------------------------------------
        var result = await validator.ValidateAsync(modelToValidate);

        // assert --------------------------------------------------------
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(errorMessage, result.Errors[0].ErrorMessage);
    }

    record PriceRange(double Min, double Max);
    public static TheoryData<string, double, string> GetNewPriceValidationFailureData()
    {
        Dictionary<string, PriceRange> _priceRanges = new()
        {
            { "boots", new PriceRange(50, 300) },
            { "kayak", new PriceRange(100, 500) },
            { "equip", new PriceRange(20, 150) }
        };
        TheoryData<string, double, string> testData = [];
        var faker = new Faker();
        foreach (var category in faker.PickRandom(_priceRanges.Keys, 2))
        {
            testData.Add(category, _priceRanges[category].Min - 0.01,
                  $"Price for {category} must be between {_priceRanges[category].Min:C} " +
                  $"and {_priceRanges[category].Max:C}");

            testData.Add(category, _priceRanges[category].Max + 0.01,
                $"Price for {category} must be between {_priceRanges[category].Min:C} " +
                $"and {_priceRanges[category].Max:C}");
        }
        return testData;
    }
}