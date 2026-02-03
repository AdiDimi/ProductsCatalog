using System.ComponentModel.DataAnnotations;

namespace AdsApi;

public record PhotoDto(string FileName, string Url);

public record CreateProductDto(
    string Name,
    string Description,
    string? Category,
    decimal? Price,
    int Stock,
    string? ImageUrl
);

public record UpdateProductDto(
    string? Name,
    string? Description,
    string? Category,
    decimal? Price,
    int? Stock,
    string? ImageUrl
);


public record Query (string? q, string? category, decimal? minPrice, decimal? maxPrice, double? lat, double? lng, double? radiusKm, int page = 1, int pageSize = 10, string? sort = "ASC");
