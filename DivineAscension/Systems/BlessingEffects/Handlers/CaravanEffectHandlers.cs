using System.Collections.Generic;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Services;
using DivineAscension.Systems.Altar;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.BlessingEffects.Handlers;

/// <summary>
///     Special effect handlers for the Caravan (Trade &amp; Wayfaring) domain.
/// </summary>
public static class CaravanEffectHandlers
{
    /// <summary>
    ///     Grants a single Caravan Shrine block item to the player when the
    ///     <c>caravan_avatar_road</c> capstone activates. Idempotent: skips the grant if the
    ///     player already has a shrine placed or carrying one in their inventory. Re-grants
    ///     on activation after deactivation (e.g., switching religions and re-unlocking).
    /// </summary>
    public class GrantsCaravanShrineEffect : ISpecialEffectHandler
    {
        internal const string ShrineBlockCode = "divineascension:caravanshrine";

        private readonly HashSet<string> _activePlayers = new();
        private readonly IPlayerProgressionDataManager _progression;
        private ILoggerWrapper? _logger;
        private IWorldService? _worldService;

        public GrantsCaravanShrineEffect(IPlayerProgressionDataManager progression)
        {
            _progression = progression;
        }

        public string EffectId => SpecialEffects.GrantsCaravanShrine;

        public void Initialize(ILoggerWrapper logger, IEventService eventService, IWorldService worldService)
        {
            _logger = logger;
            _worldService = worldService;
            _logger.Debug($"{SystemConstants.LogPrefix} Initialized {EffectId} handler");
        }

        public void ActivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Add(player.PlayerUID);
            TryGrantShrine(player);
        }

        public void DeactivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Remove(player.PlayerUID);
        }

        public void OnTick(float deltaTime)
        {
        }

        internal void TryGrantShrine(IServerPlayer player)
        {
            if (_worldService == null) return;

            var data = _progression.GetOrCreatePlayerData(player.PlayerUID);
            if (data.HasPlacedCaravanShrine) return;
            if (PlayerHasShrineInInventory(player)) return;

            var block = _worldService.World.GetBlock(new AssetLocation(ShrineBlockCode));
            if (block == null)
            {
                _logger?.Warning(
                    $"{SystemConstants.LogPrefix} Caravan Shrine block not found at {ShrineBlockCode}");
                return;
            }

            var stack = new ItemStack(block, 1);
            if (!player.InventoryManager.TryGiveItemstack(stack))
            {
                _worldService.SpawnItemEntity(stack, player.Entity.Pos.XYZ);
            }

            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup,
                LocalizationService.Instance.Get("caravanshrine.granted"),
                EnumChatType.Notification);
        }

        internal static bool PlayerHasShrineInInventory(IServerPlayer player)
        {
            foreach (var inv in player.InventoryManager.Inventories.Values)
            {
                if (inv == null) continue;
                foreach (var slot in inv)
                {
                    if (slot?.Itemstack == null) continue;
                    if (CaravanShrinePlacementHandler.IsCaravanShrineItem(slot.Itemstack))
                        return true;
                }
            }

            return false;
        }
    }
}
