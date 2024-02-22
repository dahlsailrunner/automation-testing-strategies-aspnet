using CarvedRock.Core;
using CarvedRock.Data;
using FluentValidation;

namespace CarvedRock.Domain;
public class NewProductValidator : AbstractValidator<NewProductModel>
{
    private readonly ICarvedRockRepository _repo;

    internal record PriceRange(double Min, double Max);
    internal Dictionary<string, PriceRange> _priceRanges = new()
    {
        { "boots", new PriceRange(50, 300) },
        { "kayak", new PriceRange(100, 500) },
        { "equip", new PriceRange(20, 150) }
    };

    public NewProductValidator(ICarvedRockRepository repo)
    {
        _repo = repo;

        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull().WithMessage("{PropertyName} is required.")
            .MaximumLength(50).WithMessage("{PropertyName} must not exceed 50 characters.")
            .MustAsync(NameIsUnique).WithMessage("A product with the same name already exists.");

        RuleFor(p => p.Description)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull().WithMessage("{PropertyName} is required.")
            .MaximumLength(150).WithMessage("{PropertyName} must not exceed 150 characters.");

        RuleFor(p => p.Category)
            .Must(Constants.Categories.Contains)
            .WithMessage($"Category must be one of {string.Join(",", Constants.Categories)}.");

        RuleFor(p => p.Price)
           .Must(PriceIsValid)
           .WithMessage(p => $"Price for {p.Category} must be between {_priceRanges[p.Category]!.Min:C} and {_priceRanges[p.Category]!.Max:C}");

        RuleFor(p => p.ImgUrl)
            .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute)).WithMessage("{PropertyName} must be a valid URL.")
            .MaximumLength(255).WithMessage("{PropertyName} must not exceed 255 characters.");
    }

    private bool PriceIsValid(NewProductModel ctx, double priceToValidate)
    {
        if (!_priceRanges.ContainsKey(ctx.Category)) return true; // no category defined

        var range = _priceRanges[ctx.Category];
        return priceToValidate >= range.Min && priceToValidate <= range.Max;
    }
    private async Task<bool> NameIsUnique(string name, CancellationToken token)
    {
        return await _repo.IsProductNameUniqueAsync(name);
    }
}
