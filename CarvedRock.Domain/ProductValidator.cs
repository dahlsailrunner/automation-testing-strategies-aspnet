using CarvedRock.Core;
using CarvedRock.Data;
using FluentValidation;

namespace CarvedRock.Domain;
public class NewProductValidator : AbstractValidator<NewProductModel>
{
    private readonly ICarvedRockRepository _repo;

    public NewProductValidator(ICarvedRockRepository repo)
    {
        _repo = repo;

        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull().WithMessage("{PropertyName} is required.")
            .MaximumLength(50).WithMessage("{PropertyName} must not exceed 50 characters.");

        RuleFor(e => e.Name)
            .MustAsync(NameIsUnique)
            .WithMessage("A company with the same name already exists.");

        RuleFor(p => p.Category)
            .Must(Constants.Categories.Contains)
            .WithMessage($"Category must be one of {string.Join(",", Constants.Categories)}.");
    }

    private async Task<bool> NameIsUnique(string name, CancellationToken token)
    {
        return await _repo.IsProductNameUniqueAsync(name);
    }
}
