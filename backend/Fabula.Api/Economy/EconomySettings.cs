using System.Text.Json.Serialization;
using Fabula.Api.Entities;

namespace Fabula.Api.Economy;

public record EconomySettings(
    int ChoicePriceMin,
    int ChoicePriceMax,
    int StoryPriceMin,
    int StoryPriceMax,
    Dictionary<CefrLevel, int> StoryRewards,
    int DailyGoalReward,
    Dictionary<int, int> StreakBonuses,
    int StreakFreezePrice,
    int WelcomeBonus,
    string TimeZone)
{
    public bool IsValidChoicePrice(int price) => price == 0 || (price >= ChoicePriceMin && price <= ChoicePriceMax);

    public bool IsValidStoryPrice(int price) => price == 0 || (price >= StoryPriceMin && price <= StoryPriceMax);

    public int StoryReward(CefrLevel level) => StoryRewards.GetValueOrDefault(level);

    [JsonIgnore]
    public string ChoicePriceRule => $"Ціна вибору: 0 (безкоштовно) або від {ChoicePriceMin} до {ChoicePriceMax} кристалів.";

    [JsonIgnore]
    public string StoryPriceRule => $"Ціна історії: 0 (безкоштовно) або від {StoryPriceMin} до {StoryPriceMax} кристалів.";
}
