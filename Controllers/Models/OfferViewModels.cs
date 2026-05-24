using System.ComponentModel.DataAnnotations;

namespace SneakerAgregator.Controllers.Models;

public record SizeRequest(
    [Required] string Size,
    bool Available = true
);

public record CreateOfferRequest(
    [Required] int     ProductId,
    [Required] int     StoreId,
    [Required] decimal Price,
    [Required] string  Url,
    bool InStock           = true,
    List<SizeRequest>? Sizes = null
);

public record UpdateOfferRequest(
    decimal? Price,
    string?  Url,
    bool?    InStock,
    List<SizeRequest>? Sizes
);
