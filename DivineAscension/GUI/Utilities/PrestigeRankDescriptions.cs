using DivineAscension.Models.Enum;

public static class PrestigeRankDescriptions
{
    public static string GetDescription(PrestigeRank rank) => rank switch
    {
        PrestigeRank.Fledgling => "Your religion begins its sacred journey.",
        PrestigeRank.Established => "Your religion's influence grows!",
        PrestigeRank.Renowned => "Your religion commands respect across the lands.",
        PrestigeRank.Legendary => "Your religion's legend spreads far and wide!",
        PrestigeRank.Mythic => "Your religion has achieved mythic status!",
        _ => "Your religion's prestige has increased."
    };
}