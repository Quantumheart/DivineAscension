using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.BlessingEffects.Handlers;

/// <summary>
///     Special effect handlers for Khoras (Forge & Craft) deity
/// </summary>
public static class KhorasEffectHandlers
{
    /// <summary>
    ///     Passive tool repair effect - repairs tools over time in inventory
    ///     Effect ID: passive_tool_repair_1per5min
    /// </summary>
    public class PassiveToolRepairEffect : ISpecialEffectHandler
    {
        private const int REPAIR_INTERVAL_MS = 300000; // 5 minutes in milliseconds
        private const int REPAIR_AMOUNT = 1;

        private readonly Dictionary<string, long> _lastRepairTime = new();
        private IEventService? _eventService;
        private ILogger? _logger;
        private IWorldService? _worldService;

        public string EffectId => "passive_tool_repair_1per5min";

        public void Initialize(ILogger logger, IEventService eventService, IWorldService worldService)
        {
            _logger = logger;
            _eventService = eventService;
            _worldService = worldService;
            _logger.Debug($"{SystemConstants.LogPrefix} Initialized {EffectId} handler");
        }

        public void ActivateForPlayer(IServerPlayer player)
        {
            // Initialize last repair time
            _lastRepairTime[player.PlayerUID] = _worldService!.ElapsedMilliseconds;
            _logger!.Debug($"{SystemConstants.LogPrefix} Activated {EffectId} for {player.PlayerName}");
        }

        public void DeactivateForPlayer(IServerPlayer player)
        {
            // Remove tracking
            _lastRepairTime.Remove(player.PlayerUID);
            _logger!.Debug($"{SystemConstants.LogPrefix} Deactivated {EffectId} for {player.PlayerName}");
        }

        public void OnTick(float deltaTime)
        {
            if (_worldService == null) return;

            var currentTime = _worldService.ElapsedMilliseconds;

            // Process each player with active effect
            foreach (var kvp in _lastRepairTime.ToList())
            {
                var playerUID = kvp.Key;
                var lastRepair = kvp.Value;

                // Check if enough time has passed
                if (currentTime - lastRepair < REPAIR_INTERVAL_MS) continue;

                // Get player
                var player = _worldService.GetPlayerByUID(playerUID);
                if (player?.Entity == null) continue;

                // Repair tools in inventory
                RepairPlayerTools(player);

                // Update last repair time
                _lastRepairTime[playerUID] = currentTime;
            }
        }

        private void RepairPlayerTools(IServerPlayer player)
        {
            var repairedCount = 0;

            // Get backpack inventory (main inventory bags)
            var backpack = player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            if (backpack != null) repairedCount += RepairToolsInInventory(backpack);

            // Get hotbar inventory
            var hotbar = player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);
            if (hotbar != null) repairedCount += RepairToolsInInventory(hotbar);

            if (repairedCount > 0)
            {
                _logger!.Debug(
                    $"{SystemConstants.LogPrefix} Passively repaired {repairedCount} tools for {player.PlayerName}");

                // Send notification to player
                player.SendMessage(GlobalConstants.GeneralChatGroup,
                    $"Khoras' blessing repaired {repairedCount} tool(s).", EnumChatType.Notification);
            }
        }

        private int RepairToolsInInventory(IInventory inventory)
        {
            var repairedCount = 0;

            // Iterate through all slots
            foreach (var slot in inventory)
            {
                if (slot?.Itemstack == null) continue;

                var itemstack = slot.Itemstack;

                // Check if item has durability
                if (itemstack.Collectible.Durability <= 0) continue;

                // Only repair tools, not weapons or armor
                if (!IsTool(itemstack)) continue;

                // Check if item needs repair
                var currentDurability = itemstack.Attributes.GetInt("durability", itemstack.Collectible.Durability);
                var maxDurability = itemstack.Collectible.Durability;

                if (currentDurability >= maxDurability) continue;

                // Repair the item
                var newDurability = Math.Min(currentDurability + REPAIR_AMOUNT, maxDurability);
                itemstack.Attributes.SetInt("durability", newDurability);
                slot.MarkDirty();
                repairedCount++;
            }

            return repairedCount;
        }

        /// <summary>
        ///     Check if an itemstack is a tool (not weapon or armor)
        /// </summary>
        private bool IsTool(ItemStack itemstack)
        {
            var collectible = itemstack.Collectible;
            var itemClass = collectible.GetType().Name;

            // Exclude armor/wearables first
            if (itemClass.Contains("Wearable") || itemClass.Contains("Armor")) return false;

            // Exclude weapons (but NOT tools that can be used as weapons like axes)
            if (itemClass.Contains("Sword") || itemClass.Contains("Spear") ||
                itemClass.Contains("Bow") || itemClass.Contains("Arrow")) return false;

            // Check if it's explicitly a tool (has Tool property or ToolTier > 0)
            if (collectible.Tool != null || collectible.ToolTier > 0) return true;

            // Knives and blades can be both tools and weapons, so include them if they have Tool property
            if ((itemClass.Contains("Knife") || itemClass.Contains("Blade")) && collectible.Tool == null)
                return false;

            return false;
        }
    }
}