using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services.Interfaces;
using Vintagestory.API.Common;

namespace DivineAscension.Services;

/// <summary>
/// Loads offering definitions from JSON asset files.
/// Uses focused dependencies (ILogger, IAssetManager) for better testability.
/// </summary>
public class OfferingLoader(ILogger logger, IAssetManager assetManager) : IOfferingLoader
{
    private static readonly string[] DomainFiles = { "craft", "wild", "conquest", "harvest", "stone" };

    private readonly IAssetManager
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));

    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Dictionary<DeityDomain, List<Offering>> _offeringsByDomain = new();
    private readonly Dictionary<string, Offering> _offeringsByItemCode = new();

    /// <summary>
    /// Loads all offering configurations from JSON files.
    /// Should be called during mod initialization.
    /// </summary>
    public void LoadOfferings()
    {
        _offeringsByDomain.Clear();
        _offeringsByItemCode.Clear();

        var totalLoaded = 0;
        var filesLoaded = 0;

        foreach (var domainFile in DomainFiles)
        {
            try
            {
                var offeringsFromFile = LoadOfferingsFromFile(domainFile);
                if (offeringsFromFile.Count > 0)
                {
                    totalLoaded += offeringsFromFile.Count;
                    filesLoaded++;
                    _logger.Debug(
                        $"[DivineAscension OfferingLoader] Loaded {offeringsFromFile.Count} offerings from {domainFile}.json");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(
                    $"[DivineAscension OfferingLoader] Failed to load offerings from {domainFile}.json: {ex.Message}");
            }
        }

        if (filesLoaded > 0)
        {
            _logger.Notification(
                $"[DivineAscension OfferingLoader] Successfully loaded {totalLoaded} offerings from {filesLoaded} files");
        }
        else
        {
            _logger.Warning(
                "[DivineAscension OfferingLoader] No offering files were loaded. Prayer offerings will not be available.");
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<Offering> GetOfferingsForDomain(DeityDomain domain)
    {
        return _offeringsByDomain.TryGetValue(domain, out var offerings)
            ? offerings.AsReadOnly()
            : Array.Empty<Offering>();
    }

    /// <inheritdoc />
    public Offering? FindOfferingByItemCode(string itemCode, DeityDomain domain)
    {
        if (string.IsNullOrWhiteSpace(itemCode))
            return null;

        // Build lookup key: domain + itemCode (case-insensitive)
        var lookupKey = $"{domain}:{itemCode}".ToLowerInvariant();
        return _offeringsByItemCode.TryGetValue(lookupKey, out var offering) ? offering : null;
    }

    /// <summary>
    /// Loads offerings from a specific domain JSON file.
    /// </summary>
    private List<Offering> LoadOfferingsFromFile(string domainFileName)
    {
        var offerings = new List<Offering>();
        var assetPath = $"config/offerings/{domainFileName}.json";

        var asset = _assetManager.Get(new AssetLocation("divineascension", assetPath));
        if (asset == null)
        {
            _logger.Debug($"[DivineAscension OfferingLoader] Asset not found: {assetPath}");
            return offerings;
        }

        var json = Encoding.UTF8.GetString(asset.Data);
        var fileDto = JsonSerializer.Deserialize<OfferingFileDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (fileDto == null)
        {
            _logger.Warning($"[DivineAscension OfferingLoader] Failed to parse {domainFileName}.json");
            return offerings;
        }

        // Validate and convert domain
        if (!TryParseDomain(fileDto.Domain, out var domain))
        {
            _logger.Error(
                $"[DivineAscension OfferingLoader] Invalid domain '{fileDto.Domain}' in {domainFileName}.json");
            return offerings;
        }

        foreach (var dto in fileDto.Offerings)
        {
            try
            {
                var offering = ConvertToOffering(dto);
                if (offering != null)
                {
                    offerings.Add(offering);

                    // Index by domain
                    if (!_offeringsByDomain.ContainsKey(domain))
                        _offeringsByDomain[domain] = new List<Offering>();
                    _offeringsByDomain[domain].Add(offering);

                    // Index by item codes for fast lookup
                    foreach (var itemCode in offering.ItemCodes)
                    {
                        var lookupKey = $"{domain}:{itemCode}".ToLowerInvariant();
                        _offeringsByItemCode[lookupKey] = offering;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    $"[DivineAscension OfferingLoader] Failed to convert offering '{dto.Name}': {ex.Message}");
            }
        }

        return offerings;
    }

    /// <summary>
    /// Converts an OfferingJsonDto to an Offering model.
    /// </summary>
    private Offering? ConvertToOffering(OfferingJsonDto dto)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            _logger.Warning("[DivineAscension OfferingLoader] Offering has empty Name, skipping");
            return null;
        }

        if (dto.ItemCodes == null || dto.ItemCodes.Count == 0)
        {
            _logger.Warning($"[DivineAscension OfferingLoader] Offering '{dto.Name}' has no item codes, skipping");
            return null;
        }

        // Validate tier (1-3)
        if (dto.Tier < 1 || dto.Tier > 3)
        {
            _logger.Warning(
                $"[DivineAscension OfferingLoader] Offering '{dto.Name}' has invalid tier {dto.Tier}, skipping");
            return null;
        }

        // Validate minHolySiteTier (1-3)
        if (dto.MinHolySiteTier < 1 || dto.MinHolySiteTier > 3)
        {
            _logger.Warning(
                $"[DivineAscension OfferingLoader] Offering '{dto.Name}' has invalid minHolySiteTier {dto.MinHolySiteTier}, using default (1)");
            dto.MinHolySiteTier = 1;
        }

        // Validate value (should be positive)
        if (dto.Value <= 0)
        {
            _logger.Warning(
                $"[DivineAscension OfferingLoader] Offering '{dto.Name}' has invalid value {dto.Value}, skipping");
            return null;
        }

        var offering = new Offering(
            dto.Name,
            dto.ItemCodes.AsReadOnly(),
            dto.Tier,
            dto.Value,
            dto.MinHolySiteTier,
            dto.Description ?? string.Empty
        );

        return offering;
    }

    /// <summary>
    /// Tries to parse a domain string to DeityDomain enum.
    /// </summary>
    private static bool TryParseDomain(string domainStr, out DeityDomain domain)
    {
        return Enum.TryParse(domainStr, ignoreCase: true, out domain);
    }
}