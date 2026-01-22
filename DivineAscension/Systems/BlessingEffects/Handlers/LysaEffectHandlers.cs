using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Services;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Systems.BlessingEffects.Handlers;

public static class LysaEffectHandlers
{
    public class FoodSpoilageEffect : ISpecialEffectHandler
    {
        private const long UpdateIntervalMs = 60000; // 1 minute
        private readonly HashSet<string> _activePlayers = new();
        private IEventService? _eventService;
        private double _lastGameTotalHours;
        private long _lastUpdateTick;
        private ILoggerWrapper? _logger;
        private IWorldService? _worldService;
        public string EffectId => SpecialEffects.FoodSpoilageReduction;

        public void Initialize(ILoggerWrapper logger, IEventService eventService, IWorldService worldService)
        {
            _logger = logger;
            _eventService = eventService;
            _worldService = worldService;
            _lastUpdateTick = _worldService.ElapsedMilliseconds;

            // Calendar might be null during early initialization
            if (_worldService.Calendar != null) _lastGameTotalHours = _worldService.Calendar.TotalHours;
        }

        public void ActivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Add(player.PlayerUID);
        }

        public void DeactivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Remove(player.PlayerUID);
        }

        public void OnTick(float deltaTime)
        {
            if (_worldService?.Calendar == null) return;

            // Initialize baseline if missing (e.g. Calendar was null during init)
            if (_lastGameTotalHours <= 0)
            {
                _lastGameTotalHours = _worldService.Calendar.TotalHours;
                _lastUpdateTick = _worldService.ElapsedMilliseconds;
                return;
            }

            var currentTick = _worldService.ElapsedMilliseconds;
            if (currentTick - _lastUpdateTick < UpdateIntervalMs) return;

            var currentTotalHours = _worldService.Calendar.TotalHours;
            var deltaHours = currentTotalHours - _lastGameTotalHours;

            _lastUpdateTick = currentTick;
            _lastGameTotalHours = currentTotalHours;

            if (deltaHours <= 0) return;

            foreach (var uid in _activePlayers.ToList())
            {
                var player = _worldService.GetPlayerByUID(uid);
                if (player?.Entity == null) continue;

                // Handle both FlatSum (base 0) and legacy WeightedSum (base 1.0) registrations
                var reduction = player.Entity.Stats.GetBlended(VintageStoryStats.FoodSpoilage);
                if (reduction > 1.0) reduction -= 1.0f; // Legacy WeightedSum: subtract base
                if (reduction <= 0) continue;

                ApplySpoilageReduction(player, (float)deltaHours, reduction);
            }
        }

        private void ApplySpoilageReduction(IServerPlayer player, float deltaHours, float reduction)
        {
            var backpack = player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            if (backpack != null) ProcessInventory(backpack, deltaHours, reduction);

            var hotbar = player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);
            if (hotbar != null) ProcessInventory(hotbar, deltaHours, reduction);
        }

        private void ProcessInventory(IInventory inventory, float deltaHours, float reduction)
        {
            foreach (var slot in inventory)
            {
                if (slot?.Itemstack?.Collectible == null) continue;

                var props = slot.Itemstack.Collectible.GetTransitionableProperties(_worldService!.World, slot.Itemstack,
                    null);
                if (props == null || props.Length == 0) continue;

                var hasPerish = false;
                foreach (var prop in props)
                    if (prop.Type == EnumTransitionType.Perish)
                    {
                        hasPerish = true;
                        break;
                    }

                if (!hasPerish) continue;

                var restoreHours = deltaHours * reduction;

                var transitionState = slot.Itemstack.Attributes.GetTreeAttribute("transitionstate");
                if (transitionState == null) continue;

                var createdTotalHours = transitionState.GetDouble("createdTotalHours");
                transitionState.SetDouble("createdTotalHours", createdTotalHours + restoreHours);

                slot.MarkDirty();
            }
        }
    }

    public class TemperatureResistanceEffect : ISpecialEffectHandler
    {
        private const long UpdateIntervalMs = 5000; // 5 seconds
        private readonly HashSet<string> _activePlayers = new();
        private IEventService? _eventService;
        private long _lastUpdateTick;
        private ILoggerWrapper? _logger;
        private IWorldService? _worldService;
        public string EffectId => SpecialEffects.TemperatureResistance;

        public void Initialize(ILoggerWrapper logger, IEventService eventService, IWorldService worldService)
        {
            _logger = logger;
            _eventService = eventService;
            _worldService = worldService;
            _lastUpdateTick = _worldService.ElapsedMilliseconds;
        }

        public void ActivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Add(player.PlayerUID);
        }

        public void DeactivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Remove(player.PlayerUID);
        }

        public void OnTick(float deltaTime)
        {
            if (_worldService == null) return;

            var currentTick = _worldService.ElapsedMilliseconds;
            if (currentTick - _lastUpdateTick < UpdateIntervalMs) return;
            _lastUpdateTick = currentTick;

            foreach (var uid in _activePlayers.ToList())
            {
                var player = _worldService.GetPlayerByUID(uid);
                if (player?.Entity == null) continue;

                // TemperatureResistance is an absolute value (degrees), not a percentage
                // Legacy WeightedSum would add 1.0 (e.g., 5.0 -> 6.0), but we don't subtract
                // since 5.0 > 1.0 and the 1 degree difference is acceptable for existing players
                var resistance = player.Entity.Stats.GetBlended(VintageStoryStats.TemperatureResistance);
                if (resistance <= 0) continue;

                var bh = player.Entity.GetBehavior<EntityBehaviorBodyTemperature>();
                if (bh != null)
                {
                    // Access body temp via WatchedAttributes since CurBodyTemp is not accessible
                    // Default to 37 (standard body temp)
                    var currentTemp = player.Entity.WatchedAttributes.GetFloat("bodyTemp", 37.0f);
                    var targetTemp = 37.0f;
                    var diff = targetTemp - currentTemp;

                    if (Math.Abs(diff) > 0.1f)
                    {
                        var nudge = Math.Sign(diff) * (resistance * 0.05f);
                        if (Math.Abs(nudge) > Math.Abs(diff)) nudge = diff;

                        player.Entity.WatchedAttributes.SetFloat("bodyTemp", currentTemp + nudge);
                        player.Entity.WatchedAttributes.MarkPathDirty("bodyTemp");
                    }
                }
            }
        }
    }

    public class RareForageChanceEffect : ISpecialEffectHandler
    {
        private readonly HashSet<string> _activePlayers = new();
        private IEventService? _eventService;
        private ILoggerWrapper? _logger;
        private IWorldService? _worldService;
        public string EffectId => SpecialEffects.RareForageChance;

        public void Initialize(ILoggerWrapper logger, IEventService eventService, IWorldService worldService)
        {
            _logger = logger;
            _eventService = eventService;
            _worldService = worldService;
            _eventService.OnBreakBlock(OnBreakBlock);
        }

        public void ActivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Add(player.PlayerUID);
        }

        public void DeactivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Remove(player.PlayerUID);
        }

        public void OnTick(float deltaTime)
        {
        }

        private void OnBreakBlock(IServerPlayer player, BlockSelection blockSel, ref float dropQuantityMultiplier,
            ref EnumHandling handling)
        {
            if (!_activePlayers.Contains(player.PlayerUID)) return;

            var block = _worldService?.World.BlockAccessor.GetBlock(blockSel.Position);
            if (block != null && (block.Code.Path.StartsWith("mushroom") || block.Code.Path.StartsWith("flower")))
                // Proxy for finding rare items: chance to double quantity
                if (_worldService!.World.Rand.NextDouble() < 0.5)
                    dropQuantityMultiplier *= 2.0f;
        }
    }
}