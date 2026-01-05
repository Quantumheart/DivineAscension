using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.Utilities;

public static class FavorRankDescriptions
{
    public static string GetDescription(FavorRank rank) => rank switch
    {
        FavorRank.Initiate => "You have begun your journey of devotion.",
        FavorRank.Disciple => "Your faith grows stronger. New blessings await!",
        FavorRank.Zealot => "Your dedication is recognized by the divine.",
        FavorRank.Champion => "You are a true champion of your deity!",
        FavorRank.Avatar => "You have achieved the highest divine favor!",
        _ => "Your devotion has been recognized."
    };
}