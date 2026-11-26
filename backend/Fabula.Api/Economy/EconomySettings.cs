using System.Text.Json.Serialization;

namespace Fabula.Api.Economy;

public record EconomySettings(int ChoicePriceMin, int ChoicePriceMax, int StoryPriceMin, int StoryPriceMax)
{
    public bool IsValidChoicePrice(int price) => price == 0 || (price >= ChoicePriceMin && price <= ChoicePriceMax);

    public bool IsValidStoryPrice(int price) => price == 0 || (price >= StoryPriceMin && price <= StoryPriceMax);

    [JsonIgnore]
    public string ChoicePriceRule => $"Ціна вибору: 0 (безкоштовно) або від {ChoicePriceMin} до {ChoicePriceMax} кристалів.";

    [JsonIgnore]
    public string StoryPriceRule => $"Ціна історії: 0 (безкоштовно) або від {StoryPriceMin} до {StoryPriceMax} кристалів.";
}
