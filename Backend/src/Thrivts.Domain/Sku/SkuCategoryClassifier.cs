namespace Thrivts.Domain.Sku;

/// <summary>Maps an item name to its sack category by keyword. Order matters and is kept from the
/// legacy app: "Track Jacket" must land in Jackets, not Tracksuits, so "jacket" is checked first.</summary>
public static class SkuCategoryClassifier
{
    public static readonly IReadOnlyDictionary<string, int> DefaultSackSizes = new Dictionary<string, int>
    {
        ["Fleeces"] = 35,
        ["Puffers"] = 30,
        ["Jackets"] = 30,
        ["Hoodies"] = 40,
        ["Sweatshirts"] = 40,
        ["Sweaters"] = 50,
        ["Knitwear"] = 50,
        ["Pants"] = 50,
        ["Jeans"] = 50,
        ["Shorts"] = 60,
        ["Shirts"] = 60,
        ["Polos"] = 60,
        ["T-Shirts"] = 70,
        ["Tracksuits"] = 35,
        ["Coats"] = 25,
        ["Mix"] = 50,
    };

    private static readonly (string[] Keywords, string Category)[] Rules =
    [
        (["fleece"], "Fleeces"),
        (["puffer", "down jacket"], "Puffers"),
        (["jacket"], "Jackets"),
        (["hoodie", "hooded"], "Hoodies"),
        (["sweatshirt", "crewneck"], "Sweatshirts"),
        (["sweater", "pullover"], "Sweaters"),
        (["cardigan", "knit"], "Knitwear"),
        (["jean", "denim"], "Jeans"),
        (["short"], "Shorts"),
        (["jogger", "cargo", "trouser", "chino", "pant"], "Pants"),
        (["polo"], "Polos"),
        (["t-shirt", "tshirt", "t shirt"], "T-Shirts"),
        (["tracksuit", "track"], "Tracksuits"),
        (["coat"], "Coats"),
        (["shirt"], "Shirts"),
    ];

    public static string Classify(string itemName)
    {
        var name = itemName.ToLowerInvariant();
        foreach (var (keywords, category) in Rules)
        {
            if (keywords.Any(name.Contains))
                return category;
        }

        return "Mix";
    }
}
