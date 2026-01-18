using System.Collections.Generic;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Systems.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.BlessingEffects.Handlers;

/// <summary>
///     Special effect handlers for Aethra (Agriculture & Light) deity
/// </summary>
public static class AethraEffectHandlers
{
    /// <summary>
    ///     Rare crop discovery effect - chance to improve harvest with rare variants
    ///     Effect ID: rare_crop_discovery
    /// </summary>
    public class RareCropDiscoveryEffect : ISpecialEffectHandler
    {
        private readonly HashSet<string> _activePlayers = new();
        private IEventService? _eventService;
        private ILogger? _logger;
        private IWorldService? _worldService;

        public string EffectId => SpecialEffects.RareCropDiscovery;

        public void Initialize(ILogger logger, IEventService eventService, IWorldService worldService)
        {
            _logger = logger;
            _eventService = eventService;
            _worldService = worldService;
            _eventService.OnBreakBlock(OnBreakBlock);
            _logger.Debug($"{SystemConstants.LogPrefix} Initialized {EffectId} handler");
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
            if (_worldService == null || !_activePlayers.Contains(player.PlayerUID)) return;

            var block = _worldService.BlockAccessor.GetBlock(blockSel.Position);
            if (block?.Code == null) return;

            var path = block.Code.Path;
            // Apply only to crop/vegetable harvests
            if (!(path.Contains("crop") || path.Contains("vegetable")) || path.Contains("harvested")) return;

            // Chance comes from blended RareCropChance stat (additive percent, e.g., 0.15 = 15%)
            var chance = player.Entity?.Stats?.GetBlended(VintageStoryStats.RareCropChance) ?? 0f;
            if (chance <= 0) return;

            if (_worldService.World.Rand.NextDouble() < chance)
            {
                // As a lightweight proxy for rare variant discovery, increase drop quantity modestly
                dropQuantityMultiplier *= 1.25f;

                // Tag the event for potential downstream integrations via a player attribute flag
                var tree = player.Entity!.WatchedAttributes.GetOrAddTreeAttribute("divineascension");
                tree.SetString("lastRareCropPath", path);
                tree.SetLong("lastRareCropGameTimeMs", _worldService.ElapsedMilliseconds);

                player.SendMessage(GlobalConstants.GeneralChatGroup,
                    Lang.Get("Aethra guides your harvest – you discover a rare crop variant!"),
                    EnumChatType.Notification);
            }
        }
    }

    /// <summary>
    ///     Never malnourished effect - prevents malnutrition penalties
    ///     Effect ID: never_malnourished
    /// </summary>
    public class NeverMalnourishedEffect : ISpecialEffectHandler
    {
        private readonly HashSet<string> _activePlayers = new();
        private IEventService? _eventService;
        private ILogger? _logger;
        private IWorldService? _worldService;

        public string EffectId => SpecialEffects.NeverMalnourished;

        public void Initialize(ILogger logger, IEventService eventService, IWorldService worldService)
        {
            _logger = logger;
            _eventService = eventService;
            _worldService = worldService;
            _logger.Debug($"{SystemConstants.LogPrefix} Initialized {EffectId} handler");
        }

        public void ActivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Add(player.PlayerUID);
            _logger!.Debug($"{SystemConstants.LogPrefix} Activated {EffectId} for {player.PlayerName}");

            // Apply immediate malnutrition immunity
            ApplyMalnutritionImmunity(player);
        }

        public void DeactivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Remove(player.PlayerUID);
            _logger!.Debug($"{SystemConstants.LogPrefix} Deactivated {EffectId} for {player.PlayerName}");

            // Remove malnutrition immunity
            RemoveMalnutritionImmunity(player);
        }

        public void OnTick(float deltaTime)
        {
            if (_worldService == null) return;

            // Periodically ensure malnutrition immunity is active
            foreach (var playerUID in _activePlayers.ToList())
            {
                var player = _worldService.GetPlayerByUID(playerUID);
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
            var tree = entity.WatchedAttributes.GetOrAddTreeAttribute("divineascension");
            tree.SetBool("never_malnourished", true);
        }

        private void RemoveMalnutritionImmunity(IServerPlayer player)
        {
            var entity = player.Entity;
            if (entity?.Stats == null) return;

            var tree = entity.WatchedAttributes.GetOrAddTreeAttribute("divineascension");
            tree.SetBool("never_malnourished", false);
        }
    }

    /// <summary>
    ///     Blessed meals effect - allows creating meals with powerful temporary buffs
    ///     Effect ID: blessed_meals
    /// </summary>
    public class BlessedMealsEffect : ISpecialEffectHandler
    {
        private readonly Dictionary<string, HashSet<string>> _activeAppliedStats = new();
        private readonly Dictionary<string, double> _activeBuffExpiry = new();
        private readonly HashSet<string> _activePlayers = new();
        private IEventService? _eventService;
        private ILogger? _logger;
        private IWorldService? _worldService;

        public string EffectId => SpecialEffects.BlessedMeals;

        public void Initialize(ILogger logger, IEventService eventService, IWorldService worldService)
        {
            _logger = logger;
            _eventService = eventService;
            _worldService = worldService;
            _logger.Debug($"{SystemConstants.LogPrefix} Initialized {EffectId} handler");

            // Subscribe to global eating event raised by Harmony patch
            EatingPatches.OnFoodEaten += OnFoodEaten;
        }

        public void ActivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Add(player.PlayerUID);
            _logger!.Debug($"{SystemConstants.LogPrefix} Activated {EffectId} for {player.PlayerName}");

            // Mark player as eligible for blessed meals for other systems to query
            var tree = player.Entity?.WatchedAttributes.GetOrAddTreeAttribute("divineascension");
            tree?.SetBool("blessedMealsEligible", true);
        }

        public void DeactivateForPlayer(IServerPlayer player)
        {
            _activePlayers.Remove(player.PlayerUID);
            _logger!.Debug($"{SystemConstants.LogPrefix} Deactivated {EffectId} for {player.PlayerName}");

            var tree = player.Entity?.WatchedAttributes.GetOrAddTreeAttribute("divineascension");
            tree?.SetBool("blessedMealsEligible", false);
        }

        public void OnTick(float deltaTime)
        {
            // Expiry is handled per-player via tracked times
            if (_worldService == null) return;

            // Remove expired buffs
            foreach (var uid in _activeBuffExpiry.Where(kv => kv.Value <= _worldService.Calendar.TotalHours)
                         .Select(kv => kv.Key).ToList())
            {
                var sp = _worldService.GetPlayerByUID(uid);
                if (sp != null) RemoveBlessedMealBuff(sp);
            }
        }

        private void OnFoodEaten(IServerPlayer player, ItemStack eatenStack)
        {
            // Forward to buff application
            if (player?.Entity == null || eatenStack == null) return;
            ApplyBlessedMealBuff(player, eatenStack);
        }

        /// <summary>
        ///     Applies blessed meal buffs when a player consumes food
        ///     This would be called from a hook into the food consumption system
        /// </summary>
        public void ApplyBlessedMealBuff(IServerPlayer player, ItemStack foodItem)
        {
            if (_worldService == null) return;
            if (!_activePlayers.Contains(player.PlayerUID)) return;

            // Calculate buff tier based on meal complexity
            var tier = CalculateFoodQuality(foodItem); // 1..3
            if (tier <= 0) return;

            var entity = player.Entity;
            if (entity?.Stats == null) return;

            // Remove any existing blessed meal buff before applying a new one
            RemoveBlessedMealBuff(player);

            // Define modifiers by tier
            var modifiers = new Dictionary<string, float>();
            switch (tier)
            {
                case 1:
                    modifiers[VintageStoryStats.WalkSpeed] = 0.05f;
                    modifiers[VintageStoryStats.HealingEffectiveness] = 0.05f;
                    modifiers[VintageStoryStats.HungerRate] = -0.05f;
                    modifiers[VintageStoryStats.CookedFoodSatiety] = 0.05f;
                    break;
                case 2:
                    modifiers[VintageStoryStats.WalkSpeed] = 0.08f;
                    modifiers[VintageStoryStats.HealingEffectiveness] = 0.08f;
                    modifiers[VintageStoryStats.HungerRate] = -0.10f;
                    modifiers[VintageStoryStats.CookedFoodSatiety] = 0.12f;
                    break;
                default:
                    modifiers[VintageStoryStats.WalkSpeed] = 0.12f;
                    modifiers[VintageStoryStats.HealingEffectiveness] = 0.12f;
                    modifiers[VintageStoryStats.HungerRate] = -0.15f;
                    modifiers[VintageStoryStats.CookedFoodSatiety] = 0.20f;
                    break;
            }

            // Duration by tier (minutes)
            var minutes = tier switch { 1 => 10f, 2 => 15f, _ => 20f };
            var expiryHours = _worldService.Calendar.TotalHours + minutes / 60f;

            var modifierId = string.Format(SystemConstants.ModifierIdFormat, player.PlayerUID) + "-meal";

            // Ensure tracking set exists
            if (!_activeAppliedStats.TryGetValue(player.PlayerUID, out var applied))
            {
                applied = new HashSet<string>();
                _activeAppliedStats[player.PlayerUID] = applied;
            }

            foreach (var kv in modifiers)
                try
                {
                    entity.Stats.Set(kv.Key, modifierId, kv.Value);
                    applied.Add(kv.Key);
                }
                catch
                {
                    // ignore stat set failures to avoid hard crash
                }

            _activeBuffExpiry[player.PlayerUID] = expiryHours;

            // Attribute flag for other systems/UX
            var tree = entity.WatchedAttributes.GetOrAddTreeAttribute("divineascension");
            tree.SetDouble("blessedMealBuffExpiryHours", expiryHours);
            tree.SetInt("blessedMealTier", tier);

            player.SendMessage(GlobalConstants.GeneralChatGroup,
                Lang.Get("Aethra's blessing empowers your meal! Effects last for {0} minutes.", (int)minutes),
                EnumChatType.Notification);
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

        private void RemoveBlessedMealBuff(IServerPlayer player)
        {
            var entity = player.Entity;
            if (entity?.Stats == null) return;

            var modifierId = string.Format(SystemConstants.ModifierIdFormat, player.PlayerUID) + "-meal";

            if (_activeAppliedStats.TryGetValue(player.PlayerUID, out var stats))
            {
                foreach (var stat in stats)
                    try
                    {
                        entity.Stats.Remove(stat, modifierId);
                    }
                    catch
                    {
                        /* ignore */
                    }

                stats.Clear();
            }

            _activeBuffExpiry.Remove(player.PlayerUID);

            var tree = entity.WatchedAttributes.GetOrAddTreeAttribute("divineascension");
            tree.RemoveAttribute("blessedMealBuffExpiryHours");
            tree.RemoveAttribute("blessedMealTier");

            player.SendMessage(GlobalConstants.GeneralChatGroup,
                Lang.Get("Aethra's meal blessing has faded."), EnumChatType.Notification);
        }
    }

    // Note: FoodSpoilageReduction effect is shared with Lysa deity
    // The implementation is in LysaEffectHandlers.FoodSpoilageEffect
    // Both Lysa and Aethra blessings use the same food_spoilage_reduction effect ID
}