using System;
using System.Reflection;
using DivineAscension.Systems.Butchering;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Patches;

/// <summary>
///     Conditional Harmony patches for Butchering mod compatibility.
///     Targets Butchering's BlockEntityButcherHook (skinning) and BlockEntityButcherTable
///     (butchering) via reflection (no compile-time dependency on Butchering). Only applied
///     when Butchering is installed.
///     Butchering replaces the vanilla harvest-a-corpse flow: dead animals are picked up as
///     ItemButcherable items and processed on workstations, so DA's existing
///     SkinningPatches (which postfix EntityBehaviorHarvestable.SetHarvested) never fire for
///     Butchering animals. These prefixes detect the workstation completion instead.
/// </summary>
public static class ButcheringPatches
{
    private static ButcheringEventEmitter? _emitter;
    private static Harmony? _harmony;
    private static bool _initialized;

    /// <summary>
    ///     Conditionally applies Harmony prefixes to Butchering workstation classes.
    ///     Called from DivineAscensionModSystem.StartServerSide after the ButcheringEventEmitter
    ///     is created.
    /// </summary>
    public static void Initialize(ICoreAPI api, ButcheringEventEmitter emitter)
    {
        if (_initialized) return;

        if (!api.ModLoader.IsModEnabled("butchering"))
        {
            api.Logger.Notification("[DivineAscension] Butchering not detected — compatibility patches skipped.");
            return;
        }

        _emitter = emitter;
        _harmony = new Harmony("com.divineascension.butchering");
        _initialized = true;

        var patched = 0;

        // BlockEntityButcherHook — skinning a hung carcass
        var hookType = ResolveType("Butchering.src.common.blockentity.BlockEntityButcherHook");
        if (hookType != null)
        {
            PatchPrefix(api, hookType, "processItem", nameof(SkinHook_ProcessItem_Prefix));
            patched++;
        }
        else
        {
            api.Logger.Warning("[DivineAscension] Could not resolve Butchering BlockEntityButcherHook type.");
        }

        // BlockEntityButcherTable — butchering a bled-out carcass
        var tableType = ResolveType("Butchering.src.common.blockentity.BlockEntityButcherTable");
        if (tableType != null)
        {
            PatchPrefix(api, tableType, "processItem", nameof(ButcherTable_ProcessItem_Prefix));
            patched++;
        }
        else
        {
            api.Logger.Warning("[DivineAscension] Could not resolve Butchering BlockEntityButcherTable type.");
        }

        api.Logger.Notification(
            $"[DivineAscension] Butchering compatibility patches applied ({patched} types patched).");
    }

    /// <summary>
    ///     Prefix on BlockEntityButcherHook.processItem — fires once when a player skins a
    ///     carcass on the skinning hook. The ItemButcherable still occupies inventory[0] at
    ///     this point (the override clones it to "skinned" state afterwards).
    /// </summary>
    public static void SkinHook_ProcessItem_Prefix(IPlayer byPlayer, int durabilitylossIn, object __instance)
    {
        RaiseWorkstationEvent(__instance, byPlayer, isSkinning: true);
    }

    /// <summary>
    ///     Prefix on BlockEntityButcherTable.processItem — fires once when a player butchers a
    ///     carcass on the butcher table. The ItemButcherable still occupies inventory[0] at
    ///     this point (the override calls TakeOutWhole() afterwards, so a postfix would see an
    ///     empty slot — a prefix is required).
    /// </summary>
    public static void ButcherTable_ProcessItem_Prefix(IPlayer byPlayer, int durabilitylossIn, object __instance)
    {
        RaiseWorkstationEvent(__instance, byPlayer, isSkinning: false);
    }

    private static void RaiseWorkstationEvent(object instance, IPlayer byPlayer, bool isSkinning)
    {
        if (_emitter == null) return;

        // Server side only — favor is awarded server-side.
        if (instance is not BlockEntity blockEntity) return;
        if (blockEntity.Api?.Side != EnumAppSide.Server) return;

        var player = byPlayer as IServerPlayer;
        if (player == null) return;

        // Read the ItemButcherable still held in workstation inventory slot 0.
        var stack = GetSlotZeroStack(instance);
        if (stack?.Collectible == null) return;

        // butcheringWorkLoad is the cleanest tier signal (entity kg weight is gone by now).
        var workload = stack.ItemAttributes["butcheringWorkLoad"].AsString("small");
        var pos = blockEntity.Pos;

        if (isSkinning)
        {
            _emitter.RaiseAnimalSkinned(player, pos, stack, workload);
        }
        else
        {
            _emitter.RaiseAnimalButchered(player, pos, stack, workload);
        }
    }

    /// <summary>
    ///     Reads inventory[0].Itemstack from a Butchering workstation via reflection. The
    ///     Butchering types are not referenced at compile time, so the public `inventory` field
    ///     (InventoryGeneric : InventoryBase) is fetched reflectively and indexed.
    /// </summary>
    private static ItemStack? GetSlotZeroStack(object instance)
    {
        var inventoryField = FindField(instance.GetType(), "inventory");
        if (inventoryField == null) return null;

        if (inventoryField.GetValue(instance) is not InventoryBase inventory) return null;

        var slot = inventory[0];
        return slot?.Itemstack;
    }

    private static FieldInfo? FindField(Type type, string name)
    {
        // Walk up the inheritance chain — GetField(string) finds inherited public fields, but
        // be explicit to handle non-public or renamed base declarations robustly.
        for (var t = type; t != null; t = t.BaseType)
        {
            var field = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) return field;
        }

        return null;
    }

    private static Type? ResolveType(string fullName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = asm.GetType(fullName);
            if (type != null) return type;
        }

        return null;
    }

    private static void PatchPrefix(ICoreAPI api, Type targetType, string methodName, string prefixName)
    {
        var original = AccessTools.Method(targetType, methodName);
        if (original == null)
        {
            api.Logger.Warning(
                $"[DivineAscension] Could not find {targetType.Name}.{methodName} — patch skipped.");
            return;
        }

        var prefixMethod = typeof(ButcheringPatches).GetMethod(prefixName,
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (prefixMethod == null)
        {
            api.Logger.Warning(
                $"[DivineAscension] Could not find prefix method {prefixName} — patch skipped.");
            return;
        }

        _harmony?.Patch(original, prefix: new HarmonyMethod(prefixMethod));
    }

    /// <summary>
    ///     Unpatches all Butchering compatibility patches and clears state.
    ///     Called from DivineAscensionModSystem.Dispose().
    /// </summary>
    public static void ClearSubscribers()
    {
        _harmony?.UnpatchAll("com.divineascension.butchering");
        _harmony = null;
        _emitter = null;
        _initialized = false;
    }
}