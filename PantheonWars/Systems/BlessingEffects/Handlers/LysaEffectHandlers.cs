using System;
using System.Collections.Generic;
using PantheonWars.Constants;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.BlessingEffects.Handlers;

public static class LysaEffectHandlers
{
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
