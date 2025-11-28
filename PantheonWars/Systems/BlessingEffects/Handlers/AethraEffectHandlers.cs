using System.Collections.Generic;
using System.Linq;
using PantheonWars.Constants;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.BlessingEffects.Handlers;

/// <summary>
///     Special effect handlers for Aethra (Agriculture & Light) deity
/// </summary>
public static class AethraEffectHandlers
{
    /// <summary>
    ///     Never malnourished effect - prevents malnutrition penalties
    ///     Effect ID: never_malnourished
    /// </summary>
    public class NeverMalnourishedEffect : ISpecialEffectHandler
    {
        private readonly HashSet<string> _activePlayers = new();
        private ICoreServerAPI? _sapi;

        public string EffectId => SpecialEffects.NeverMalnourished;

        public void Initialize(ICoreServerAPI sapi)
        {
            _sapi = sapi;
            _sapi.Logger.Debug($"{SystemConstants.LogPrefix} Initialized {EffectId} handler");
        }

        public void ActivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Add(player.PlayerUID);
            _sapi!.Logger.Debug($"{SystemConstants.LogPrefix} Activated {EffectId} for {player.PlayerName}");

            // Apply immediate malnutrition immunity
            ApplyMalnutritionImmunity(player);
        }

        public void DeactivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Remove(player.PlayerUID);
            _sapi!.Logger.Debug($"{SystemConstants.LogPrefix} Deactivated {EffectId} for {player.PlayerName}");

            // Remove malnutrition immunity
            RemoveMalnutritionImmunity(player);
        }

        public void OnTick(float deltaTime)
        {
            if (_sapi == null) return;

            // Periodically ensure malnutrition immunity is active
            foreach (var playerUID in _activePlayers.ToList())
            {
                var player = _sapi.World.PlayerByUid(playerUID) as IServerPlayer;
                if (player?.Entity == null) continue;

                // Reapply immunity to ensure it stays active
                ApplyMalnutritionImmunity(player);
            }
        }

        private void ApplyMalnutritionImmunity(IServerPlayer player)
        {
            // In Vintage Story, malnutrition is tracked via the diet/nutrition system
            // We prevent malnutrition by ensuring all diet categories stay above the threshold
            // This is done by applying a stat modifier or directly manipulating the nutrition data

            var entity = player.Entity;
            if (entity?.Stats == null) return;

            // Note: This is a placeholder implementation
            // The actual implementation depends on Vintage Story's nutrition API
            // which may require accessing entity.WatchedAttributes or Stats

            // For now, we'll mark the player as having the effect in attributes
            var tree = entity.WatchedAttributes.GetOrAddTreeAttribute("pantheonwars");
            tree.SetBool("never_malnourished", true);
        }

        private void RemoveMalnutritionImmunity(IServerPlayer player)
        {
            var entity = player.Entity;
            if (entity?.Stats == null) return;

            var tree = entity.WatchedAttributes.GetOrAddTreeAttribute("pantheonwars");
            tree.SetBool("never_malnourished", false);
        }
    }

    /// <summary>
    ///     Blessed meals effect - allows creating meals with powerful temporary buffs
    ///     Effect ID: blessed_meals
    /// </summary>
    public class BlessedMealsEffect : ISpecialEffectHandler
    {
        private readonly HashSet<string> _activePlayers = new();
        private ICoreServerAPI? _sapi;

        public string EffectId => SpecialEffects.BlessedMeals;

        public void Initialize(ICoreServerAPI sapi)
        {
            _sapi = sapi;
            _sapi.Logger.Debug($"{SystemConstants.LogPrefix} Initialized {EffectId} handler");

            // Register event listener for food consumption
            _sapi.Event.PlayerNowPlaying += OnPlayerJoin;
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
            // No periodic updates needed for this effect
        }

        private void OnPlayerJoin(IServerPlayer player)
        {
            // When a player joins, register food consumption listener
            // This is a placeholder - actual implementation requires hooking into food consumption
            // The Vintage Story API doesn't have a direct "OnEat" event, so we'd need to:
            // 1. Monitor inventory changes for food items
            // 2. Or hook into the satiety/hunger system changes
            // 3. Or use player.Entity.OnInteract to detect eating animations
        }

        /// <summary>
        ///     Applies blessed meal buffs when a player consumes food
        ///     This would be called from a hook into the food consumption system
        /// </summary>
        public void ApplyBlessedMealBuff(IServerPlayer player, ItemStack foodItem)
        {
            if (!_activePlayers.Contains(player.PlayerUID)) return;

            // Calculate buff strength based on food quality/complexity
            var buffStrength = CalculateFoodQuality(foodItem);
            if (buffStrength <= 0) return;

            // Apply temporary buffs
            var entity = player.Entity;
            if (entity?.Stats == null) return;

            // Apply buffs via stat modifiers
            // Duration: 10-30 minutes depending on food quality
            var durationSeconds = 600 + (buffStrength * 120); // 10-30 minutes

            // Buffs could include:
            // +5-15% movement speed
            // +5-15% max health
            // +10-30% satiety efficiency
            // +5-15% damage

            player.SendMessage(GlobalConstants.GeneralChatGroup,
                $"Aethra's blessing empowers your meal with divine energy!", EnumChatType.Notification);
        }

        private int CalculateFoodQuality(ItemStack foodItem)
        {
            // Determine food complexity/quality
            // Simple foods (bread, porridge) = 1
            // Complex foods (stews, pies) = 2
            // Gourmet foods (advanced meals) = 3

            var foodCode = foodItem.Collectible.Code.Path;

            if (foodCode.Contains("stew") || foodCode.Contains("soup") || foodCode.Contains("meal"))
                return 3; // Gourmet

            if (foodCode.Contains("pie") || foodCode.Contains("cooked") || foodCode.Contains("roasted"))
                return 2; // Complex

            if (foodCode.Contains("bread") || foodCode.Contains("porridge") || foodCode.Contains("basic"))
                return 1; // Simple

            return 0; // Not eligible
        }
    }

    /// <summary>
    ///     Temporary health buff from meals - provides +5% max health temporarily
    ///     Effect ID: temp_health_buff_5
    /// </summary>
    public class TempHealthBuffEffect : ISpecialEffectHandler
    {
        private const float BuffDurationSeconds = 1200f; // 20 minutes
        private const float HealthMultiplier = 0.05f; // +5%

        private readonly Dictionary<string, long> _buffExpiry = new();
        private ICoreServerAPI? _sapi;

        public string EffectId => SpecialEffects.TempHealthBuff5;

        public void Initialize(ICoreServerAPI sapi)
        {
            _sapi = sapi;
            _sapi.Logger.Debug($"{SystemConstants.LogPrefix} Initialized {EffectId} handler");
        }

        public void ActivateForPlayer(IServerPlayer player)
        {
            // This effect is triggered by eating meals, not by having the blessing
            // So activation just marks the player as eligible
            _sapi!.Logger.Debug($"{SystemConstants.LogPrefix} Player {player.PlayerName} is now eligible for {EffectId}");
        }

        public void DeactivateForPlayer(IServerPlayer player)
        {
            // Remove any active buff
            RemoveBuff(player);
            _sapi!.Logger.Debug($"{SystemConstants.LogPrefix} Deactivated {EffectId} for {player.PlayerName}");
        }

        public void OnTick(float deltaTime)
        {
            if (_sapi == null) return;

            var currentTime = _sapi.World.Calendar.TotalHours;

            // Check for expired buffs
            foreach (var kvp in _buffExpiry.ToList())
            {
                var playerUID = kvp.Key;
                var expiryTime = kvp.Value;

                if (currentTime >= expiryTime)
                {
                    var player = _sapi.World.PlayerByUid(playerUID) as IServerPlayer;
                    if (player != null)
                    {
                        RemoveBuff(player);
                    }
                    _buffExpiry.Remove(playerUID);
                }
            }
        }

        /// <summary>
        ///     Applies the temporary health buff when a meal is consumed
        ///     This would be called from the meal consumption logic
        /// </summary>
        public void ApplyBuffFromMeal(IServerPlayer player)
        {
            var entity = player.Entity;
            if (entity?.Stats == null) return;

            // Calculate expiry time
            var expiryTime = _sapi!.World.Calendar.TotalHours + (BuffDurationSeconds / 3600f);
            _buffExpiry[player.PlayerUID] = (long)(expiryTime * 1000); // Convert to milliseconds

            // Apply health buff via stat modifier
            var tree = entity.WatchedAttributes.GetOrAddTreeAttribute("pantheonwars");
            tree.SetFloat("temp_health_buff", HealthMultiplier);
            tree.SetDouble("temp_health_buff_expiry", expiryTime);

            player.SendMessage(GlobalConstants.GeneralChatGroup,
                $"Your meal grants you +{HealthMultiplier * 100}% health for {BuffDurationSeconds / 60} minutes!",
                EnumChatType.Notification);

            _sapi.Logger.Debug($"{SystemConstants.LogPrefix} Applied temp health buff to {player.PlayerName}");
        }

        private void RemoveBuff(IServerPlayer player)
        {
            var entity = player.Entity;
            if (entity == null) return;

            var tree = entity.WatchedAttributes.GetOrAddTreeAttribute("pantheonwars");
            tree.RemoveAttribute("temp_health_buff");
            tree.RemoveAttribute("temp_health_buff_expiry");

            player.SendMessage(GlobalConstants.GeneralChatGroup,
                "Your temporary health buff has expired.", EnumChatType.Notification);
        }
    }

    // Note: FoodSpoilageReduction effect is shared with Lysa deity
    // The implementation is in LysaEffectHandlers.FoodSpoilageEffect
    // Both Lysa and Aethra blessings use the same food_spoilage_reduction effect ID
}
