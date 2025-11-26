using System;
using System.Collections.Generic;
using System.Linq;
using PantheonWars.Constants;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.BlessingEffects.Handlers;

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
        private ICoreServerAPI? _sapi;

        public string EffectId => "passive_tool_repair_1per5min";

        public void Initialize(ICoreServerAPI sapi)
        {
            _sapi = sapi;
            _sapi.Logger.Debug($"{SystemConstants.LogPrefix} Initialized {EffectId} handler");
        }

        public void ActivateForPlayer(IServerPlayer player)
        {
            // Initialize last repair time
            _lastRepairTime[player.PlayerUID] = _sapi!.World.ElapsedMilliseconds;
            _sapi.Logger.Debug($"{SystemConstants.LogPrefix} Activated {EffectId} for {player.PlayerName}");
        }

        public void DeactivateForPlayer(IServerPlayer player)
        {
            // Remove tracking
            _lastRepairTime.Remove(player.PlayerUID);
            _sapi!.Logger.Debug($"{SystemConstants.LogPrefix} Deactivated {EffectId} for {player.PlayerName}");
        }

        public void OnTick(float deltaTime)
        {
            if (_sapi == null) return;

            var currentTime = _sapi.World.ElapsedMilliseconds;

            // Process each player with active effect
            foreach (var kvp in _lastRepairTime.ToList())
            {
                var playerUID = kvp.Key;
                var lastRepair = kvp.Value;

                // Check if enough time has passed
                if (currentTime - lastRepair < REPAIR_INTERVAL_MS) continue;

                // Get player
                var player = _sapi.World.PlayerByUid(playerUID) as IServerPlayer;
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
            if (backpack != null)
            {
                repairedCount += RepairToolsInInventory(backpack);
            }

            // Get hotbar inventory
            var hotbar = player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);
            if (hotbar != null)
            {
                repairedCount += RepairToolsInInventory(hotbar);
            }

            if (repairedCount > 0)
            {
                _sapi!.Logger.Debug(
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

                // Check if item is a tool or weapon with durability
                if (itemstack.Collectible.Durability <= 0) continue;

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
    }
    
}
