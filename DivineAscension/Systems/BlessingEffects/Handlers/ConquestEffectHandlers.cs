using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.Constants;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Systems.BlessingEffects.Handlers;

/// <summary>
///     Special effect handlers for Conquest domain
/// </summary>
public static class ConquestEffectHandlers
{
    /// <summary>
    ///     Battle Fury effect - damage increases temporarily after each kill
    ///     Effect ID: battle_fury
    /// </summary>
    public class BattleFuryEffect : ISpecialEffectHandler
    {
        private const int FURY_DURATION_MS = 30000; // 30 seconds
        private const float FURY_DAMAGE_BONUS = 0.05f; // 5% per stack
        private const int MAX_FURY_STACKS = 5;

        private readonly Dictionary<string, FuryState> _playerFuryStates = new();
        private ICoreServerAPI? _sapi;

        public string EffectId => SpecialEffects.BattleFury;

        public void Initialize(ICoreServerAPI sapi)
        {
            _sapi = sapi;
            _sapi.Event.OnEntityDeath += OnEntityDeath;
            _sapi.Logger.Debug($"{SystemConstants.LogPrefix} Initialized {EffectId} handler");
        }

        public void ActivateForPlayer(IServerPlayer player)
        {
            _playerFuryStates[player.PlayerUID] = new FuryState();
            _sapi!.Logger.Debug($"{SystemConstants.LogPrefix} Activated {EffectId} for {player.PlayerName}");
        }

        public void DeactivateForPlayer(IServerPlayer player)
        {
            if (_playerFuryStates.TryGetValue(player.PlayerUID, out var state) && state.Stacks > 0)
            {
                // Remove any active fury bonuses
                RemoveFuryBonus(player, state.Stacks);
            }

            _playerFuryStates.Remove(player.PlayerUID);
            _sapi!.Logger.Debug($"{SystemConstants.LogPrefix} Deactivated {EffectId} for {player.PlayerName}");
        }

        public void OnTick(float deltaTime)
        {
            if (_sapi == null) return;

            var currentTime = _sapi.World.ElapsedMilliseconds;

            foreach (var kvp in _playerFuryStates.ToList())
            {
                var playerUID = kvp.Key;
                var state = kvp.Value;

                if (state.Stacks <= 0 || state.LastKillTime == 0) continue;

                // Check if fury has expired
                if (currentTime - state.LastKillTime >= FURY_DURATION_MS)
                {
                    var player = _sapi.World.PlayerByUid(playerUID) as IServerPlayer;
                    if (player?.Entity != null)
                    {
                        RemoveFuryBonus(player, state.Stacks);
                        player.SendMessage(GlobalConstants.GeneralChatGroup,
                            "Your battle fury fades.", EnumChatType.Notification);
                    }

                    state.Stacks = 0;
                    state.LastKillTime = 0;
                }
            }
        }

        private void OnEntityDeath(Entity? entity, DamageSource? damageSource)
        {
            if (_sapi == null || entity == null || damageSource == null) return;
            if (entity is EntityPlayer) return; // Don't trigger on player deaths

            var killer = damageSource.GetCauseEntity();
            if (killer is not EntityPlayer { Player: IServerPlayer player }) return;

            if (!_playerFuryStates.TryGetValue(player.PlayerUID, out var state)) return;

            var previousStacks = state.Stacks;
            state.Stacks = Math.Min(state.Stacks + 1, MAX_FURY_STACKS);
            state.LastKillTime = _sapi.World.ElapsedMilliseconds;

            if (state.Stacks > previousStacks)
            {
                ApplyFuryBonus(player, state.Stacks);
                player.SendMessage(GlobalConstants.GeneralChatGroup,
                    $"Battle Fury! ({state.Stacks}/{MAX_FURY_STACKS} stacks)", EnumChatType.Notification);
            }
        }

        private void ApplyFuryBonus(IServerPlayer player, int totalStacks)
        {
            var bonus = FURY_DAMAGE_BONUS * totalStacks;
            player.Entity?.Stats.Set("meleeWeaponsDamage", "battlefury", bonus, false);
        }

        private void RemoveFuryBonus(IServerPlayer player, int stacksToRemove)
        {
            player.Entity?.Stats.Remove("meleeWeaponsDamage", "battlefury");
        }

        private class FuryState
        {
            public int Stacks { get; set; }
            public long LastKillTime { get; set; }
        }
    }

    /// <summary>
    ///     Bloodlust effect - heal a small amount when defeating enemies
    ///     Effect ID: bloodlust
    /// </summary>
    public class BloodlustEffect : ISpecialEffectHandler
    {
        private const float HEAL_PERCENT = 0.05f; // 5% of max health

        private readonly HashSet<string> _activePlayers = new();
        private ICoreServerAPI? _sapi;

        public string EffectId => SpecialEffects.Bloodlust;

        public void Initialize(ICoreServerAPI sapi)
        {
            _sapi = sapi;
            _sapi.Event.OnEntityDeath += OnEntityDeath;
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
            // No tick processing needed
        }

        private void OnEntityDeath(Entity? entity, DamageSource? damageSource)
        {
            if (_sapi == null || entity == null || damageSource == null) return;
            if (entity is EntityPlayer) return;

            var killer = damageSource.GetCauseEntity();
            if (killer is not EntityPlayer { Player: IServerPlayer player }) return;

            if (!_activePlayers.Contains(player.PlayerUID)) return;

            var healthBehavior = player.Entity?.GetBehavior<EntityBehaviorHealth>();
            if (healthBehavior == null) return;

            var healAmount = healthBehavior.MaxHealth * HEAL_PERCENT;
            healthBehavior.Health = Math.Min(healthBehavior.Health + healAmount, healthBehavior.MaxHealth);

            player.SendMessage(GlobalConstants.GeneralChatGroup,
                $"Bloodlust! Restored {healAmount:F1} health.", EnumChatType.Notification);
        }
    }

    /// <summary>
    ///     Warcry effect - intimidate nearby hostile creatures
    ///     Effect ID: warcry
    /// </summary>
    public class WarcryEffect : ISpecialEffectHandler
    {
        private readonly HashSet<string> _activePlayers = new();
        private ICoreServerAPI? _sapi;

        public string EffectId => SpecialEffects.Warcry;

        public void Initialize(ICoreServerAPI sapi)
        {
            _sapi = sapi;
            _sapi.Logger.Debug($"{SystemConstants.LogPrefix} Initialized {EffectId} handler");
        }

        public void ActivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Add(player.PlayerUID);
            // Warcry provides a passive intimidation aura - enemies deal less damage
            player.Entity?.Stats.Set("animalSeekingRange", "warcry", -0.15f, false);
            _sapi!.Logger.Debug($"{SystemConstants.LogPrefix} Activated {EffectId} for {player.PlayerName}");
        }

        public void DeactivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Remove(player.PlayerUID);
            player.Entity?.Stats.Remove("animalSeekingRange", "warcry");
            _sapi!.Logger.Debug($"{SystemConstants.LogPrefix} Deactivated {EffectId} for {player.PlayerName}");
        }

        public void OnTick(float deltaTime)
        {
            // No tick processing needed - passive effect
        }
    }

    /// <summary>
    ///     Last Stand effect - gain damage reduction when health drops below 25%
    ///     Effect ID: last_stand
    /// </summary>
    public class LastStandEffect : ISpecialEffectHandler
    {
        private const float HEALTH_THRESHOLD = 0.25f; // 25% health
        private const float DAMAGE_REDUCTION_BONUS = 0.20f; // 20% damage reduction

        private readonly Dictionary<string, bool> _lastStandActive = new();
        private ICoreServerAPI? _sapi;

        public string EffectId => SpecialEffects.LastStand;

        public void Initialize(ICoreServerAPI sapi)
        {
            _sapi = sapi;
            _sapi.Logger.Debug($"{SystemConstants.LogPrefix} Initialized {EffectId} handler");
        }

        public void ActivateForPlayer(IServerPlayer player)
        {
            _lastStandActive[player.PlayerUID] = false;
            _sapi!.Logger.Debug($"{SystemConstants.LogPrefix} Activated {EffectId} for {player.PlayerName}");
        }

        public void DeactivateForPlayer(IServerPlayer player)
        {
            if (_lastStandActive.TryGetValue(player.PlayerUID, out var wasActive) && wasActive)
            {
                player.Entity?.Stats.Remove("damageReduction", "laststand");
            }

            _lastStandActive.Remove(player.PlayerUID);
            _sapi!.Logger.Debug($"{SystemConstants.LogPrefix} Deactivated {EffectId} for {player.PlayerName}");
        }

        public void OnTick(float deltaTime)
        {
            if (_sapi == null) return;

            foreach (var kvp in _lastStandActive.ToList())
            {
                var playerUID = kvp.Key;
                var isActive = kvp.Value;

                var player = _sapi.World.PlayerByUid(playerUID) as IServerPlayer;
                if (player?.Entity == null) continue;

                var healthBehavior = player.Entity.GetBehavior<EntityBehaviorHealth>();
                if (healthBehavior == null) continue;

                var healthPercent = healthBehavior.Health / healthBehavior.MaxHealth;
                var shouldBeActive = healthPercent <= HEALTH_THRESHOLD;

                if (shouldBeActive && !isActive)
                {
                    // Activate last stand
                    player.Entity.Stats.Set("damageReduction", "laststand", DAMAGE_REDUCTION_BONUS, false);
                    _lastStandActive[playerUID] = true;
                    player.SendMessage(GlobalConstants.GeneralChatGroup,
                        "Last Stand activated! Damage reduction increased.", EnumChatType.Notification);
                }
                else if (!shouldBeActive && isActive)
                {
                    // Deactivate last stand
                    player.Entity.Stats.Remove("damageReduction", "laststand");
                    _lastStandActive[playerUID] = false;
                    player.SendMessage(GlobalConstants.GeneralChatGroup,
                        "Last Stand deactivated.", EnumChatType.Notification);
                }
            }
        }
    }
}