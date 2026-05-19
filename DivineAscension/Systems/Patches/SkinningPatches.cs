using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Systems.Patches;

[HarmonyPatch]
public static class SkinningPatches
{
    /// <summary>
    /// Event fired when an animal is skinned/butchered by a player.
    /// Provides the player, entity, and entity weight for favor calculation.
    /// </summary>
    public static event Action<IServerPlayer, Entity, float>? OnAnimalSkinned;

    private static TagSetFast _huntableAnimalTags;

    public static void Initialize(ICoreAPI api)
    {
        _huntableAnimalTags = api.EntityTagRegistry.CreateTagSet(["huntable", "animal"]);
    }

    public static void ClearSubscribers()
    {
        OnAnimalSkinned = null;
    }

    /// <summary>
    /// Patch EntityBehaviorHarvestable.SetHarvested - called when a player successfully completes harvesting a corpse.
    /// This fires exactly once per corpse per player, making it the perfect detection point for skinning/butchering.
    /// </summary>
    [HarmonyPatch(typeof(EntityBehaviorHarvestable), nameof(EntityBehaviorHarvestable.SetHarvested))]
    [HarmonyPostfix]
    public static void Postfix_SetHarvested(
        EntityBehaviorHarvestable __instance,
        IPlayer byPlayer)
    {
        // Only process on server side
        if (__instance.entity?.World?.Api?.Side != EnumAppSide.Server) return;

        // Check if the player is a valid server player
        if (byPlayer is not IServerPlayer serverPlayer) return;

        // Check if the entity is an animal (huntable + animal tags)
        if (!_huntableAnimalTags.IsFullyContainedIn(__instance.entity.Tags)) return;

        // Get entity weight for favor calculation
        float weight = __instance.entity.Properties?.Weight ?? 0f;

        // Fire event for favor tracker
        OnAnimalSkinned?.Invoke(serverPlayer, __instance.entity, weight);
    }
}