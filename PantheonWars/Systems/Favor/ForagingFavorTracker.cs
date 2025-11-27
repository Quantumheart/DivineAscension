using System;
using System.Collections.Generic;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.Favor;

public class ForagingFavorTracker(IPlayerReligionDataManager playerReligionDataManager, ICoreServerAPI sapi, FavorSystem favorSystem) : IFavorTracker, IDisposable
{
    public DeityType DeityType { get; } = DeityType.Lysa;
    private readonly IPlayerReligionDataManager _playerReligionDataManager = playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));
    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
    private readonly FavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));
    
    private readonly HashSet<string> _lysaFollowers = new();

    // Track berry inventory counts to detect harvesting
    private readonly Dictionary<string, Dictionary<string, int>> _playerBerryInventory = new();

    public void Initialize()
    {
        _sapi.Event.BreakBlock += OnBlockBroken;

        // Register periodic check for berry harvesting (every 2 seconds)
        _sapi.Event.RegisterGameTickListener(OnBerryHarvestTick, 2000);

        // Cache followers
        RefreshFollowerCache();

        _playerReligionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
        _playerReligionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesReligion;
    }

    private void RefreshFollowerCache()
    {
        foreach (var player in _sapi.World.AllOnlinePlayers)
        {
             UpdateFollower(player.PlayerUID);
        }
    }

    private void OnPlayerDataChanged(string playerUID) => UpdateFollower(playerUID);
    
    private void UpdateFollower(string playerUID)
    {
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(playerUID);
        if (religionData?.ActiveDeity == DeityType)
            _lysaFollowers.Add(playerUID);
        else
            _lysaFollowers.Remove(playerUID);
    }

    private void OnPlayerLeavesReligion(IServerPlayer player, string religionUID)
    {
        _lysaFollowers.Remove(player.PlayerUID);
    }

    private void OnBlockBroken(IServerPlayer player, BlockSelection blockSel, ref float dropQuantityMultiplier, ref EnumHandling handling)
    {
        if (!_lysaFollowers.Contains(player.PlayerUID)) return;

        var block = _sapi.World.BlockAccessor.GetBlock(blockSel.Position);
        if (IsForageBlock(block))
        {
            // Award 0.5 favor per forage (breaking mushrooms, flowers, etc.)
            _favorSystem.AwardFavorForAction(player, "foraging " + GetForageName(block), 0.5f);
        }
    }

    /// <summary>
    /// Periodically checks for berry harvesting by tracking berry inventory increases
    /// </summary>
    private void OnBerryHarvestTick(float dt)
    {
        foreach (var player in _sapi.World.AllOnlinePlayers)
        {
            if (player is not IServerPlayer serverPlayer) continue;
            if (!_lysaFollowers.Contains(serverPlayer.PlayerUID)) continue;

            // Check player's inventory for berries
            CheckBerryInventory(serverPlayer);
        }
    }

    private void CheckBerryInventory(IServerPlayer player)
    {
        var playerUID = player.PlayerUID;

        // Initialize tracking for this player if needed
        if (!_playerBerryInventory.ContainsKey(playerUID))
        {
            _playerBerryInventory[playerUID] = new Dictionary<string, int>();
        }

        var currentBerryCounts = new Dictionary<string, int>();

        // Check both backpack AND hotbar inventories
        var backpack = player.InventoryManager?.GetOwnInventory("backpack");
        var hotbar = player.InventoryManager?.GetOwnInventory("hotbar");

        // Count berries in backpack
        if (backpack != null)
        {
            CountBerriesInInventory(backpack, currentBerryCounts);
        }

        // Count berries in hotbar
        if (hotbar != null)
        {
            CountBerriesInInventory(hotbar, currentBerryCounts);
        }

        // Compare with previous counts to detect new berries
        var previousCounts = _playerBerryInventory[playerUID];

        foreach (var (berryType, currentCount) in currentBerryCounts)
        {
            int previousCount = previousCounts.GetValueOrDefault(berryType, 0);

            // If berry count increased, player harvested some berries
            if (currentCount > previousCount)
            {
                // Award favor (0.5 per harvest action, not per berry)
                _favorSystem.AwardFavorForAction(player, "harvesting " + GetBerryDisplayName(berryType), 0.5f);
            }
        }

        // Update tracked counts
        _playerBerryInventory[playerUID] = currentBerryCounts;
    }

    private void CountBerriesInInventory(IInventory inventory, Dictionary<string, int> berryCounts)
    {
        foreach (var slot in inventory)
        {
            if (slot?.Itemstack == null) continue;

            string itemPath = slot.Itemstack.Collectible?.Code?.Path ?? "";
            if (IsBerryItem(itemPath))
            {
                if (!berryCounts.ContainsKey(itemPath))
                    berryCounts[itemPath] = 0;

                berryCounts[itemPath] += slot.Itemstack.StackSize;
            }
        }
    }

    private bool IsBerryItem(string itemPath)
    {
        return itemPath.Contains("berry") || itemPath.Contains("currant");
    }

    private string GetBerryDisplayName(string itemPath)
    {
        if (itemPath.Contains("blackberry")) return "blackberries";
        if (itemPath.Contains("blueberry")) return "blueberries";
        if (itemPath.Contains("cranberry")) return "cranberries";
        if (itemPath.Contains("redcurrant")) return "redcurrants";
        if (itemPath.Contains("whitecurrant")) return "whitecurrants";
        return "berries";
    }
    
    private bool IsForageBlock(Block block)
    {
        if (block?.Code == null) return false;
        string path = block.Code.Path;

        // Forageable blocks that are broken (mushrooms, flowers, seaweed)
        // Note: Berry bushes are NOT broken, they're interacted with
        return path.StartsWith("mushroom") ||
               path.StartsWith("flower") ||
               path.StartsWith("seaweed");
    }

    private string GetForageName(Block block)
    {
        if (block?.Code == null) return "plants";
        string path = block.Code.Path;

        if (path.Contains("mushroom")) return "mushrooms";
        if (path.Contains("flower")) return "flowers";
        if (path.Contains("seaweed")) return "seaweed";

        return "plants";
    }

    public void Dispose()
    {
        _sapi.Event.BreakBlock -= OnBlockBroken;
        _playerReligionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerReligionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesReligion;
        _lysaFollowers.Clear();
        _playerBerryInventory.Clear();
    }
}
