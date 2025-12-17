using System.Collections.Generic;
using PantheonWars.Constants;
using PantheonWars.Systems.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.BlessingEffects.Handlers;

/// <summary>
///     Special effect handlers for Gaia (Pottery & Clay)
/// </summary>
public static class GaiaEffectHandlers
{
    /// <summary>
    ///     Pottery Batch Completion Bonus: Chance to craft a duplicate pottery item on completion
    ///     Effect ID: pottery_batch_completion_bonus
    ///     Hooks into ClayFormingPatches.OnClayFormingFinished
    /// </summary>
    public class PotteryBatchCompletionEffect : ISpecialEffectHandler
    {
        private readonly HashSet<string> _activePlayers = new();
        private ICoreServerAPI? _sapi;

        public string EffectId => SpecialEffects.PotteryBatchCompletionBonus;

        public void Initialize(ICoreServerAPI sapi)
        {
            _sapi = sapi;
            ClayFormingPatches.OnClayFormingFinished += HandleClayFormingFinished;
            _sapi.Logger.Debug($"{SystemConstants.LogPrefix} Initialized {EffectId} handler");
        }

        public void ActivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Add(player.PlayerUID);
            _sapi!.Logger.Debug($"{SystemConstants.LogPrefix} Activated {EffectId} for {player.PlayerName}");
        }

        public void DeactivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Remove(player.PlayerUID);
            _sapi!.Logger.Debug($"{SystemConstants.LogPrefix} Deactivated {EffectId} for {player.PlayerName}");
        }

        public void OnTick(float deltaTime)
        {
        }

        private void HandleClayFormingFinished(IServerPlayer player, ItemStack resultStack)
        {
            if (_sapi == null) return;
            if (!_activePlayers.Contains(player.PlayerUID)) return;

            var chance = player.Entity?.Stats?.GetBlended(VintageStoryStats.PotteryBatchCompletionChance) ?? 0f;
            if (chance <= 0) return;

            if (_sapi.World.Rand.NextDouble() < chance)
            {
                // Duplicate the resulting stack
                var duplicate = resultStack.Clone();

                // Try to give to player inventory, else drop near player
                var invMan = player.InventoryManager;
                var given = invMan.TryGiveItemstack(duplicate);
                if (!given)
                {
                    // Drop at player's position as a fallback
                    var pos = player.Entity?.ServerPos.XYZ ?? player.Entity?.Pos.XYZ;
                    _sapi.World.SpawnItemEntity(duplicate, pos);
                }

                // Feedback
                player.SendMessage(GlobalConstants.GeneralChatGroup,
                    Lang.Get("Gaia blesses your craft – you produced an extra {0}!", resultStack.GetName()),
                    EnumChatType.Notification);
            }
        }
    }
}