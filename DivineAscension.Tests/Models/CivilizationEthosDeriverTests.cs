using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.Models;
using DivineAscension.Models.Enum;

namespace DivineAscension.Tests.Models;

[ExcludeFromCodeCoverage]
public class CivilizationEthosDeriverTests
{
    [Theory]
    [InlineData(DeityDomain.Craft, CivilizationEthos.Mercantile, LocalizationKeys.CIVILIZATION_EPITHET_CRAFT)]
    [InlineData(DeityDomain.Conquest, CivilizationEthos.Martial, LocalizationKeys.CIVILIZATION_EPITHET_CONQUEST)]
    [InlineData(DeityDomain.Wild, CivilizationEthos.Mystic, LocalizationKeys.CIVILIZATION_EPITHET_WILD)]
    [InlineData(DeityDomain.Harvest, CivilizationEthos.Ascetic, LocalizationKeys.CIVILIZATION_EPITHET_HARVEST)]
    [InlineData(DeityDomain.Stone, CivilizationEthos.Sovereign, LocalizationKeys.CIVILIZATION_EPITHET_STONE)]
    [InlineData(DeityDomain.Caravan, CivilizationEthos.Mercantile, LocalizationKeys.CIVILIZATION_EPITHET_CARAVAN)]
    [InlineData(DeityDomain.None, CivilizationEthos.Sovereign, LocalizationKeys.CIVILIZATION_EPITHET_DEFAULT)]
    public void Derive_MapsDomainToEthosAndEpithetKey(
        DeityDomain domain, CivilizationEthos expectedEthos, string expectedKey)
    {
        var (ethos, key) = CivilizationEthosDeriver.Derive(domain);

        Assert.Equal(expectedEthos, ethos);
        Assert.Equal(expectedKey, key);
    }
}
