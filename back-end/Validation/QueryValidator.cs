using AdsApi.Endpoints;
using FluentValidation;

namespace AdsApi.Validation;

public class ProductsSearchQueryValidator : AbstractValidator<Query>
{
    public ProductsSearchQueryValidator()
    {
        RuleFor(x => x.page)
            .GreaterThanOrEqualTo(1).WithMessage("page must be >= 1");

        RuleFor(x => x.pageSize)
            .InclusiveBetween(1, 100).WithMessage("pageSize must be between 1 and 100");

        RuleFor(x => x)
            .Must(q => !(q.minPrice.HasValue && q.maxPrice.HasValue) || q.minPrice!.Value <= q.maxPrice!.Value)
            .WithMessage("minPrice must be less than or equal to maxPrice");

        RuleFor(x => x)
            .Must(q => (q.lat is null && q.lng is null && q.radiusKm is null) || (q.lat is not null && q.lng is not null))
            .WithMessage("lat and lng must be provided together (or both omitted)");

        When(x => x.radiusKm is not null, () =>
        {
            RuleFor(x => x.radiusKm!.Value).GreaterThan(0).WithMessage("radiusKm must be > 0");
        });

        RuleFor(x => x.sort)
            .Must(s => string.IsNullOrEmpty(s) || s is "Asc" or "Desc" or "recent")
            .WithMessage("sort must be one of: Asc, Desc, recent");
    }
}
