using System;
using System.Linq;
using System.Text;
using DivineAscension.API.Interfaces;
using DivineAscension.Commands.Parsers;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Commands;

/// <summary>
/// Chat commands for holy site management.
/// Provides commands for creating, querying, and removing holy sites.
/// </summary>
public class HolySiteCommands(
    IChatCommandService commandService,
    IHolySiteManager holySiteManager,
    IReligionManager religionManager)
{
    private readonly IChatCommandService _commandService =
        commandService ?? throw new ArgumentNullException(nameof(commandService));

    private readonly IHolySiteManager _holySiteManager =
        holySiteManager ?? throw new ArgumentNullException(nameof(holySiteManager));

    private readonly IReligionManager _religionManager =
        religionManager ?? throw new ArgumentNullException(nameof(religionManager));

    public void RegisterCommands()
    {
        _commandService.Create("holysite")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_HOLYSITE_DESC))
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.chat)
            // /holysite info [site_name]
            .BeginSubCommand("info")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_HOLYSITE_INFO_DESC))
            .WithArgs(_commandService.Parsers.OptionalQuotedString("site_name"))
            .HandleWith(OnHolySiteInfo)
            .EndSubCommand()
            // /holysite list
            .BeginSubCommand("list")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_HOLYSITE_LIST_DESC))
            .HandleWith(OnListHolySites)
            .EndSubCommand()
            // /holysite nearby [radius]
            .BeginSubCommand("nearby")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_HOLYSITE_NEARBY_DESC))
            .WithArgs(_commandService.Parsers.OptionalInt("radius", 10))
            .HandleWith(OnNearbyHolySites)
            .EndSubCommand();
    }

    private TextCommandResult OnHolySiteInfo(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Player not found");

        var siteName = args.Parsers[0].GetValue() as string;
        HolySiteData? site;

        if (string.IsNullOrWhiteSpace(siteName))
        {
            // No name provided, check current location
            var pos = player.Entity.Pos.AsBlockPos;
            site = _holySiteManager.GetHolySiteAtPosition(pos);

            if (site == null)
                return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.HOLYSITE_NOT_IN_SITE));
        }
        else
        {
            // Find by name (check player's religion first)
            var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
            if (religion != null)
            {
                site = _holySiteManager.GetReligionHolySites(religion.ReligionUID)
                    .FirstOrDefault(s => s.SiteName.Equals(siteName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // Not in religion, search all sites
                site = _holySiteManager.GetAllHolySites()
                    .FirstOrDefault(s => s.SiteName.Equals(siteName, StringComparison.OrdinalIgnoreCase));
            }

            if (site == null)
                return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.HOLYSITE_NOT_FOUND,
                    siteName));
        }

        // Build info message
        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.HOLYSITE_INFO_HEADER, site.SiteName));
        sb.AppendLine(
            $"Tier: {site.GetTier()} (Volume: {site.GetTotalVolume():N0} blocks³, {site.Areas.Count} area(s))");
        sb.AppendLine($"Prayer Bonus: {site.GetPrayerMultiplier():F1}x");
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.HOLYSITE_INFO_FOUNDER, site.FounderName));
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.HOLYSITE_INFO_CREATED,
            site.CreationDate.ToString("yyyy-MM-dd")));

        var center = site.GetCenter();
        sb.AppendLine($"Center: ({center.X}, {center.Y}, {center.Z})");

        // Show altar position if this is an altar-based site
        if (site.IsAltarSite() && site.AltarPosition != null)
        {
            var altarPos = site.AltarPosition.ToBlockPos();
            sb.AppendLine($"Altar: ({altarPos.X}, {altarPos.Y}, {altarPos.Z})");
        }

        return TextCommandResult.Success(sb.ToString());
    }

    private TextCommandResult OnListHolySites(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Player not found");

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null)
            return TextCommandResult.Error(LocalizationService.Instance.Get(LocalizationKeys.HOLYSITE_NOT_MEMBER));

        var sites = _holySiteManager.GetReligionHolySites(religion.ReligionUID);
        if (sites.Count == 0)
            return TextCommandResult.Success(LocalizationService.Instance.Get(LocalizationKeys.HOLYSITE_LIST_EMPTY));

        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.HOLYSITE_LIST_HEADER, religion.ReligionName));

        foreach (var site in sites.OrderBy(s => s.SiteName))
        {
            var center = site.GetCenter();
            sb.AppendLine(
                $"- {site.SiteName} (Tier {site.GetTier()}, {site.GetTotalVolume():N0} blocks³) at ({center.X}, {center.Z})");
        }

        return TextCommandResult.Success(sb.ToString());
    }

    private TextCommandResult OnNearbyHolySites(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Player not found");

        var radius = (int)(args.Parsers[0].GetValue() ?? 10);
        if (radius < 1 || radius > 100)
            return TextCommandResult.Error("Radius must be between 1 and 100 chunks");

        var playerPos = player.Entity.Pos.AsBlockPos;

        var nearbySites = _holySiteManager.GetAllHolySites()
            .Select(site => new
            {
                Site = site,
                Center = site.GetCenter(),
                Distance = 0.0 // Calculate below
            })
            .Select(x => new
            {
                x.Site,
                x.Center,
                Distance = Math.Sqrt(
                    Math.Pow((x.Center.X - playerPos.X) / 256.0, 2) +
                    Math.Pow((x.Center.Z - playerPos.Z) / 256.0, 2))
            })
            .Where(x => x.Distance <= radius)
            .OrderBy(x => x.Distance)
            .ToList();

        if (nearbySites.Count == 0)
            return TextCommandResult.Success(LocalizationService.Instance.Get(LocalizationKeys.HOLYSITE_NEARBY_EMPTY));

        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.HOLYSITE_NEARBY_HEADER, radius));

        foreach (var item in nearbySites)
        {
            var religion = _religionManager.GetReligion(item.Site.ReligionUID);
            var religionName = religion?.ReligionName ?? "Unknown";

            sb.AppendLine(
                $"- {item.Site.SiteName} ({religionName}) - Tier {item.Site.GetTier()} at ({item.Center.X}, {item.Center.Z}) - Distance: {(int)item.Distance} chunks");
        }

        return TextCommandResult.Success(sb.ToString());
    }
}