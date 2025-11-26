using System;
using System.Collections.Generic;
using System.Linq;
using PantheonWars.Constants;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace PantheonWars.Systems.BlessingEffects.Handlers;

public static class LysaEffectHandlers
{
    public class FoodSpoilageEffect : ISpecialEffectHandler
    {
        public string EffectId => SpecialEffects.FoodSpoilageReduction;
        private ICoreServerAPI? _sapi;
        private readonly HashSet<string> _activePlayers = new();
        private long _lastUpdateTick;
        private double _lastGameTotalHours;
        private const long UpdateIntervalMs = 60000; // 1 minute

        public void Initialize(ICoreServerAPI sapi)
        {
            _sapi = sapi;
            _lastUpdateTick = _sapi.World.ElapsedMilliseconds;
            _lastGameTotalHours = _sapi.World.Calendar.TotalHours;
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
            if (_sapi == null) return;

            var currentTick = _sapi.World.ElapsedMilliseconds;
            if (currentTick - _lastUpdateTick < UpdateIntervalMs) return;

            var currentTotalHours = _sapi.World.Calendar.TotalHours;
            var deltaHours = currentTotalHours - _lastGameTotalHours;
            
            _lastUpdateTick = currentTick;
            _lastGameTotalHours = currentTotalHours;

            if (deltaHours <= 0) return;

            foreach (var uid in _activePlayers.ToList())
            {
                var player = _sapi.World.PlayerByUid(uid) as IServerPlayer;
                if (player?.Entity == null) continue;

                var reduction = player.Entity.Stats.GetBlended(VintageStoryStats.FoodSpoilage);
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
                
                var props = slot.Itemstack.Collectible.GetTransitionableProperties(_sapi!.World, slot.Itemstack, null);
                if (props == null || props.Length == 0) continue;

                bool hasPerish = false;
                foreach (var prop in props)
                {
                    if (prop.Type == EnumTransitionType.Perish)
                    {
                        hasPerish = true;
                        break;
                    }
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
        public string EffectId => SpecialEffects.TemperatureResistance;
        private ICoreServerAPI? _sapi;
        private readonly HashSet<string> _activePlayers = new();
        private long _lastUpdateTick;
        private const long UpdateIntervalMs = 5000; // 5 seconds

        public void Initialize(ICoreServerAPI sapi)
        {
            _sapi = sapi;
            _lastUpdateTick = _sapi.World.ElapsedMilliseconds;
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
            if (_sapi == null) return;
            
            var currentTick = _sapi.World.ElapsedMilliseconds;
            if (currentTick - _lastUpdateTick < UpdateIntervalMs) return;
            _lastUpdateTick = currentTick;

            foreach (var uid in _activePlayers.ToList())
            {
                var player = _sapi.World.PlayerByUid(uid) as IServerPlayer;
                if (player?.Entity == null) continue;

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
        public string EffectId => SpecialEffects.RareForageChance;
        private ICoreServerAPI? _sapi;
        private readonly HashSet<string> _activePlayers = new();

        public void Initialize(ICoreServerAPI sapi)
        {
            _sapi = sapi;
            _sapi.Event.BreakBlock += OnBreakBlock;
        }

        public void ActivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Add(player.PlayerUID);
        }

        public void DeactivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Remove(player.PlayerUID);
        }

        public void OnTick(float deltaTime) { }

        private void OnBreakBlock(IServerPlayer player, BlockSelection blockSel, ref float dropQuantityMultiplier, ref EnumHandling handling)
        {
            if (!_activePlayers.Contains(player.PlayerUID)) return;
            
            var block = _sapi?.World.BlockAccessor.GetBlock(blockSel.Position);
            if (block != null && (block.Code.Path.StartsWith("mushroom") || block.Code.Path.StartsWith("flower")))
            {
                // Proxy for finding rare items: chance to double quantity
                if (_sapi!.World.Rand.NextDouble() < 0.5)
                {
                    dropQuantityMultiplier *= 2.0f;
                }
            }
        }
    }
}
