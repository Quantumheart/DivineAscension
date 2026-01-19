using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Data;
using DivineAscension.Network.HolySite;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Networking.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Networking.Server;

/// <summary>
/// Handles holy site-related network requests from clients.
/// Supports three actions: "list" (all sites with optional domain filter),
/// "religion_sites" (sites for a specific religion), and "detail" (single site details).
/// </summary>
[ExcludeFromCodeCoverage]
public class HolySiteNetworkHandler : IServerNetworkHandler
{
    private readonly IHolySiteManager _holySiteManager;
    private readonly ILogger _logger;
    private readonly INetworkService _networkService;
    private readonly IReligionManager _religionManager;

    public HolySiteNetworkHandler(
        ILogger logger,
        IHolySiteManager holySiteManager,
        IReligionManager religionManager,
        INetworkService networkService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _holySiteManager = holySiteManager ?? throw new ArgumentNullException(nameof(holySiteManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
    }

    public void RegisterHandlers()
    {
        _networkService.RegisterMessageHandler<HolySiteRequestPacket>(OnHolySiteRequest);
    }

    public void Dispose()
    {
        // No resources to dispose
    }

    private void OnHolySiteRequest(IServerPlayer fromPlayer, HolySiteRequestPacket packet)
    {
        try
        {
            HolySiteResponsePacket response = packet.Action switch
            {
                "list" => HandleListAction(packet.DomainFilter),
                "religion_sites" => HandleReligionSitesAction(packet.ReligionUID),
                "detail" => HandleDetailAction(packet.SiteUID),
                _ => new HolySiteResponsePacket() // Empty response for unknown action
            };

            _networkService.SendToPlayer(fromPlayer, response);
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error handling holy site request: {ex.Message}");
            _networkService.SendToPlayer(fromPlayer, new HolySiteResponsePacket());
        }
    }

    /// <summary>
    /// Handles "list" action - returns all holy sites with optional domain filter.
    /// </summary>
    private HolySiteResponsePacket HandleListAction(string domainFilter)
    {
        var allSites = _holySiteManager.GetAllHolySites();

        // Apply domain filter if provided
        if (!string.IsNullOrWhiteSpace(domainFilter))
        {
            allSites = allSites
                .Where(site =>
                {
                    var religion = _religionManager.GetReligion(site.ReligionUID);
                    return religion != null && religion.Domain.ToString() == domainFilter;
                })
                .ToList();
        }

        var siteInfos = allSites
            .Select(site => MapToSiteInfo(site))
            .Where(info => info != null)
            .ToList();

        return new HolySiteResponsePacket(siteInfos!);
    }

    /// <summary>
    /// Handles "religion_sites" action - returns all sites for a specific religion.
    /// </summary>
    private HolySiteResponsePacket HandleReligionSitesAction(string religionUID)
    {
        if (string.IsNullOrWhiteSpace(religionUID))
        {
            _logger.Warning("[DivineAscension] Religion UID required for religion_sites action");
            return new HolySiteResponsePacket();
        }

        var sites = _holySiteManager.GetReligionHolySites(religionUID);

        var siteInfos = sites
            .Select(site => MapToSiteInfo(site))
            .Where(info => info != null)
            .ToList();

        return new HolySiteResponsePacket(siteInfos!);
    }

    /// <summary>
    /// Handles "detail" action - returns detailed information for a single holy site.
    /// </summary>
    private HolySiteResponsePacket HandleDetailAction(string siteUID)
    {
        if (string.IsNullOrWhiteSpace(siteUID))
        {
            _logger.Warning("[DivineAscension] Site UID required for detail action");
            return new HolySiteResponsePacket();
        }

        var site = _holySiteManager.GetHolySite(siteUID);
        if (site == null)
        {
            _logger.Warning($"[DivineAscension] Holy site {siteUID} not found");
            return new HolySiteResponsePacket();
        }

        var detailInfo = MapToDetailInfo(site);
        if (detailInfo == null)
        {
            return new HolySiteResponsePacket();
        }

        return new HolySiteResponsePacket(detailInfo);
    }

    /// <summary>
    /// Maps HolySiteData to HolySiteInfo for list display.
    /// </summary>
    private HolySiteResponsePacket.HolySiteInfo? MapToSiteInfo(HolySiteData site)
    {
        var religion = _religionManager.GetReligion(site.ReligionUID);
        if (religion == null)
        {
            _logger.Warning($"[DivineAscension] Religion {site.ReligionUID} not found for holy site {site.SiteUID}");
            return null;
        }

        return new HolySiteResponsePacket.HolySiteInfo
        {
            SiteUID = site.SiteUID,
            SiteName = site.SiteName,
            ReligionUID = site.ReligionUID,
            ReligionName = religion.ReligionName,
            Domain = religion.Domain.ToString(),
            Tier = site.GetTier(),
            Volume = site.GetTotalVolume(),
            AreaCount = site.Areas.Count,
            TerritoryMultiplier = site.GetTerritoryMultiplier(),
            PrayerMultiplier = site.GetPrayerMultiplier()
        };
    }

    /// <summary>
    /// Maps HolySiteData to HolySiteDetailInfo for detailed view.
    /// </summary>
    private HolySiteResponsePacket.HolySiteDetailInfo? MapToDetailInfo(HolySiteData site)
    {
        var religion = _religionManager.GetReligion(site.ReligionUID);
        if (religion == null)
        {
            _logger.Warning($"[DivineAscension] Religion {site.ReligionUID} not found for holy site {site.SiteUID}");
            return null;
        }

        var center = site.GetCenter();

        var detailInfo = new HolySiteResponsePacket.HolySiteDetailInfo
        {
            SiteUID = site.SiteUID,
            SiteName = site.SiteName,
            ReligionUID = site.ReligionUID,
            ReligionName = religion.ReligionName,
            Domain = religion.Domain.ToString(),
            FounderUID = site.FounderUID,
            FounderName = site.FounderName,
            CreationDate = site.CreationDate,
            Tier = site.GetTier(),
            Volume = site.GetTotalVolume(),
            XZArea = site.GetTotalXZArea(),
            TerritoryMultiplier = site.GetTerritoryMultiplier(),
            PrayerMultiplier = site.GetPrayerMultiplier(),
            Center = new HolySiteResponsePacket.CenterPosition
            {
                X = center.X,
                Y = center.Y,
                Z = center.Z
            },
            Areas = site.Areas.Select(area => new HolySiteResponsePacket.ChunkInfo
            {
                X1 = area.X1,
                Y1 = area.Y1,
                Z1 = area.Z1,
                X2 = area.X2,
                Y2 = area.Y2,
                Z2 = area.Z2,
                Volume = area.GetVolume(),
                XZArea = area.GetXZArea()
            }).ToList()
        };

        return detailInfo;
    }
}