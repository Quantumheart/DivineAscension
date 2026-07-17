using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DivineAscension.Systems.Toolsmith;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Patches;

/// <summary>
///     Conditional Harmony patches for Toolsmith compatibility.
///     Targets Toolsmith's BlockGrindstone, BlockWorkbench, ItemWhetstone, and
///     TinkeringUtility via reflection (no compile-time dependency on Toolsmith).
///     Only applied when Toolsmith is installed.
///     Toolsmith's custom classes override interaction methods without calling base,
///     so the JSON-patch + BlockBehavior approach doesn't work — direct Harmony
///     postfixes are required.
/// </summary>
public static class ToolsmithPatches
{
    private static ToolsmithEventEmitter? _emitter;
    private static Harmony? _harmony;
    private static bool _initialized;

    // Per-player tracking to fire events once per interaction session
    private static readonly HashSet<string> GrindstoneSharpeningFired = new();
    private static readonly HashSet<string> GrindstoneDisassemblyFired = new();
    private static readonly HashSet<string> WhetstoneSharpeningFired = new();

    // Per-player cooldown for workbench reforging (covers multiple hammer strikes)
    private static readonly Dictionary<string, DateTime> WorkbenchReforgeCooldown = new();
    private static readonly HashSet<string> WorkbenchDisassemblyFired = new();

    // Workbench slot indices (from Toolsmith's WorkbenchSlots enum)
    private const int ViseSlot = 6;
    private const int ReforgeStagingSlot = 7;

    private static readonly TimeSpan WorkbenchCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     Conditionally applies Harmony patches to Toolsmith classes.
    ///     Called from DivineAscensionModSystem.StartServerSide after the ToolsmithEventEmitter is created.
    /// </summary>
    public static void Initialize(ICoreAPI api, ToolsmithEventEmitter emitter)
    {
        if (_initialized) return;

        if (!api.ModLoader.IsModEnabled("toolsmith"))
        {
            api.Logger.Notification("[DivineAscension] Toolsmith not detected — compatibility patches skipped.");
            return;
        }

        _emitter = emitter;
        _harmony = new Harmony("com.divineascension.toolsmith");
        _initialized = true;

        var patched = 0;

        // BlockGrindstone — sharpening and disassembly
        var grindstoneType = ResolveType("Toolsmith.ToolTinkering.Blocks.BlockGrindstone");
        if (grindstoneType != null)
        {
            PatchMethod(api, grindstoneType, "OnBlockInteractStart", nameof(Grindstone_InteractStart_Postfix));
            PatchMethod(api, grindstoneType, "OnBlockInteractStep", nameof(Grindstone_InteractStep_Postfix));
            PatchMethod(api, grindstoneType, "OnBlockInteractStop", nameof(Grindstone_InteractStop_Postfix));
            PatchMethod(api, grindstoneType, "OnBlockInteractCancel", nameof(Grindstone_InteractCancel_Postfix));
            patched++;
        }
        else
        {
            api.Logger.Warning("[DivineAscension] Could not resolve Toolsmith BlockGrindstone type.");
        }

        // BlockWorkbench — disassembly and reforging (assembly is detected via TinkeringUtility)
        var workbenchType = ResolveType("Toolsmith.ToolTinkering.Blocks.BlockWorkbench");
        if (workbenchType != null)
        {
            PatchMethod(api, workbenchType, "OnBlockInteractStart", nameof(Workbench_InteractStart_Postfix));
            PatchMethod(api, workbenchType, "OnBlockInteractStep", nameof(Workbench_InteractStep_Postfix));
            patched++;
        }
        else
        {
            api.Logger.Warning("[DivineAscension] Could not resolve Toolsmith BlockWorkbench type.");
        }

        // ItemWhetstone — whetstone sharpening
        var whetstoneType = ResolveType("Toolsmith.ToolTinkering.Items.ItemWhetstone");
        if (whetstoneType != null)
        {
            PatchMethod(api, whetstoneType, "OnHeldInteractStart", nameof(Whetstone_InteractStart_Postfix));
            PatchMethod(api, whetstoneType, "OnHeldInteractStep", nameof(Whetstone_InteractStep_Postfix));
            PatchMethod(api, whetstoneType, "OnHeldInteractStop", nameof(Whetstone_InteractStop_Postfix));
            patched++;
        }
        else
        {
            api.Logger.Warning("[DivineAscension] Could not resolve Toolsmith ItemWhetstone type.");
        }

        // TinkeringUtility — tool assembly detection (covers workbench + in-hand assembly)
        var tinkeringUtilityType = ResolveType("Toolsmith.ToolTinkering.TinkeringUtility");
        if (tinkeringUtilityType != null)
        {
            // TryCraftToolFromSlots — workbench assembly (returns ItemStack, null if failed)
            PatchMethod(api, tinkeringUtilityType, "TryCraftToolFromSlots",
                nameof(TryCraftToolFromSlots_Postfix));
            // AssembleFullTool — in-hand assembly (void, replaces bundle with tool in slot)
            PatchMethod(api, tinkeringUtilityType, "AssembleFullTool",
                nameof(AssembleFullTool_Postfix));
            patched++;
        }
        else
        {
            api.Logger.Warning("[DivineAscension] Could not resolve Toolsmith TinkeringUtility type.");
        }

        api.Logger.Notification(
            $"[DivineAscension] Toolsmith compatibility patches applied ({patched} types patched).");
    }

    // ─── Grindstone postfixes ──────────────────────────────────────────────

    public static void Grindstone_InteractStart_Postfix(IWorldAccessor world, IPlayer byPlayer,
        BlockSelection blockSel)
    {
        if (world.Side != EnumAppSide.Server) return;
        GrindstoneSharpeningFired.Remove(byPlayer.PlayerUID);
        GrindstoneDisassemblyFired.Remove(byPlayer.PlayerUID);
    }

    public static void Grindstone_InteractStep_Postfix(float secondsUsed, IWorldAccessor world,
        IPlayer byPlayer, BlockSelection blockSel)
    {
        if (world.Side != EnumAppSide.Server || _emitter == null) return;

        var player = byPlayer as IServerPlayer;
        if (player == null) return;

        var hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if (hotbarSlot?.Itemstack == null) return;

        // Disassembly: shift + hold > 4.5s (matches Toolsmith's threshold)
        if (byPlayer.Entity.Controls.ShiftKey && secondsUsed > 4.5f)
        {
            if (GrindstoneDisassemblyFired.Add(player.PlayerUID))
            {
                _emitter.RaiseToolDisassembled(player, blockSel.Position, hotbarSlot.Itemstack);
            }

            return;
        }

        // Sharpening: no shift, fire once per session
        if (!byPlayer.Entity.Controls.ShiftKey && secondsUsed > 0.1f)
        {
            if (GrindstoneSharpeningFired.Add(player.PlayerUID))
            {
                _emitter.RaiseToolSharpened(player, blockSel.Position, hotbarSlot.Itemstack);
            }
        }
    }

    public static void Grindstone_InteractStop_Postfix(IWorldAccessor world, IPlayer byPlayer)
    {
        if (world.Side != EnumAppSide.Server) return;
        GrindstoneSharpeningFired.Remove(byPlayer.PlayerUID);
        GrindstoneDisassemblyFired.Remove(byPlayer.PlayerUID);
    }

    public static void Grindstone_InteractCancel_Postfix(IWorldAccessor world, IPlayer byPlayer)
    {
        if (world.Side != EnumAppSide.Server) return;
        GrindstoneSharpeningFired.Remove(byPlayer.PlayerUID);
        GrindstoneDisassemblyFired.Remove(byPlayer.PlayerUID);
    }

    // ─── Workbench postfixes ───────────────────────────────────────────────

    // Detect reforging (hammer on reforge slot). Assembly is detected via TinkeringUtility.
    public static void Workbench_InteractStart_Postfix(IWorldAccessor world, IPlayer byPlayer,
        BlockSelection blockSel)
    {
        if (world.Side != EnumAppSide.Server || _emitter == null) return;

        var player = byPlayer as IServerPlayer;
        if (player == null) return;

        var slotIndex = blockSel?.SelectionBoxIndex ?? -1;
        var hasHammer = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible?.Tool ==
                        EnumTool.Hammer;

        if (hasHammer != true || byPlayer.Entity.Controls.ShiftKey) return;

        // Reforging: hammer on reforge staging slot
        if (slotIndex == ReforgeStagingSlot)
        {
            if (IsCooldownElapsed(WorkbenchReforgeCooldown, player.PlayerUID))
            {
                var hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                _emitter.RaiseToolReforged(player, blockSel.Position,
                    hotbarSlot?.Itemstack ?? new ItemStack());
            }
        }
    }

    // Detect disassembly (shift + hold on vise)
    public static void Workbench_InteractStep_Postfix(float secondsUsed, IWorldAccessor world,
        IPlayer byPlayer, BlockSelection blockSel)
    {
        if (world.Side != EnumAppSide.Server || _emitter == null) return;

        var player = byPlayer as IServerPlayer;
        if (player == null) return;

        if (!byPlayer.Entity.Controls.ShiftKey) return;

        var slotIndex = blockSel?.SelectionBoxIndex ?? -1;
        if (slotIndex != ViseSlot) return;

        if (secondsUsed > 4.5f)
        {
            if (WorkbenchDisassemblyFired.Add(player.PlayerUID))
            {
                var hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                if (hotbarSlot?.Itemstack != null)
                {
                    _emitter.RaiseToolDisassembled(player, blockSel.Position, hotbarSlot.Itemstack);
                }
            }
        }
    }

    // ─── Whetstone postfixes ───────────────────────────────────────────────

    public static void Whetstone_InteractStart_Postfix(ItemSlot slot, EntityAgent byEntity, bool firstEvent)
    {
        if (byEntity?.Api?.Side != EnumAppSide.Server || !firstEvent) return;

        var player = (byEntity as EntityPlayer)?.Player as IServerPlayer;
        if (player != null)
        {
            WhetstoneSharpeningFired.Remove(player.PlayerUID);
        }
    }

    public static void Whetstone_InteractStep_Postfix(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        if (byEntity?.Api?.Side != EnumAppSide.Server || _emitter == null) return;

        var player = (byEntity as EntityPlayer)?.Player as IServerPlayer;
        if (player == null) return;

        // The whetstone is in the offhand; the tool being sharpened is in the main hand
        var mainHandSlot = byEntity.RightHandItemSlot;
        if (mainHandSlot?.Itemstack == null) return;

        // Fire once per sharpening session
        if (secondsUsed > 0.1f && WhetstoneSharpeningFired.Add(player.PlayerUID))
        {
            _emitter.RaiseToolSharpened(player,
                new BlockPos((int)byEntity.Pos.X, (int)byEntity.Pos.Y, (int)byEntity.Pos.Z),
                mainHandSlot.Itemstack);
        }
    }

    public static void Whetstone_InteractStop_Postfix(EntityAgent byEntity)
    {
        if (byEntity?.Api?.Side != EnumAppSide.Server) return;

        var player = (byEntity as EntityPlayer)?.Player as IServerPlayer;
        if (player != null)
        {
            WhetstoneSharpeningFired.Remove(player.PlayerUID);
        }
    }

    // ─── TinkeringUtility postfixes (tool assembly) ────────────────────────

    // Workbench assembly: TryCraftToolFromSlots returns the crafted ItemStack (null if failed).
    // No player parameter — resolve from nearby players at the workbench position.
    public static void TryCraftToolFromSlots_Postfix(ItemStack __result, IWorldAccessor world,
        BlockSelection blockSel)
    {
        if (world.Side != EnumAppSide.Server || _emitter == null) return;
        if (__result == null) return; // Assembly failed
        if (blockSel?.Position == null) return;

        var player = FindNearestPlayer(world, blockSel.Position, 8);
        if (player == null) return;

        _emitter.RaiseToolAssembled(player, blockSel.Position, __result);
    }

    // In-hand assembly: AssembleFullTool is void and replaces the bundle in the slot with the tool.
    // Player is available via byEntity. Detect success by checking if the slot now holds a tool
    // (not a bundle, not null).
    public static void AssembleFullTool_Postfix(ItemSlot bundleSlot, EntityAgent byEntity)
    {
        if (byEntity?.Api?.Side != EnumAppSide.Server || _emitter == null) return;

        // If the slot is null or still contains a "tinkertoolparts" bundle, assembly failed
        var stack = bundleSlot?.Itemstack;
        if (stack == null) return;
        if (stack.Collectible?.Code?.Path?.Contains("tinkertoolparts") == true) return;

        var player = (byEntity as EntityPlayer)?.Player as IServerPlayer;
        if (player == null) return;

        _emitter.RaiseToolAssembled(player,
            new BlockPos((int)byEntity.Pos.X, (int)byEntity.Pos.Y, (int)byEntity.Pos.Z),
            stack);
    }

    // ─── Helpers ───────────────────────────────────────────────────────────

    private static Type? ResolveType(string fullName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = asm.GetType(fullName);
            if (type != null) return type;
        }

        return null;
    }

    private static void PatchMethod(ICoreAPI api, Type targetType, string methodName, string postfixName)
    {
        var original = AccessTools.Method(targetType, methodName);
        if (original == null)
        {
            api.Logger.Warning(
                $"[DivineAscension] Could not find {targetType.Name}.{methodName} — patch skipped.");
            return;
        }

        var postfixMethod = typeof(ToolsmithPatches).GetMethod(postfixName,
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (postfixMethod == null)
        {
            api.Logger.Warning(
                $"[DivineAscension] Could not find postfix method {postfixName} — patch skipped.");
            return;
        }

        _harmony?.Patch(original, postfix: new HarmonyMethod(postfixMethod));
    }

    private static bool IsCooldownElapsed(Dictionary<string, DateTime> cooldowns, string playerUid)
    {
        if (cooldowns.TryGetValue(playerUid, out var last))
        {
            if (DateTime.UtcNow - last < WorkbenchCooldown)
                return false;
        }

        cooldowns[playerUid] = DateTime.UtcNow;
        return true;
    }

    private static IServerPlayer? FindNearestPlayer(IWorldAccessor world, BlockPos pos, int radius)
    {
        var bestDistSq = (radius + 0.5) * (radius + 0.5);
        IServerPlayer? best = null;

        foreach (var p in world.AllPlayers)
        {
            if (p is not IServerPlayer sp) continue;
            var epos = sp.Entity?.Pos?.AsBlockPos;
            if (epos == null) continue;

            var dx = epos.X - pos.X;
            var dy = epos.Y - pos.Y;
            var dz = epos.Z - pos.Z;
            var distSq = dx * (double)dx + dy * (double)dy + dz * (double)dz;
            if (distSq <= bestDistSq)
            {
                bestDistSq = distSq;
                best = sp;
            }
        }

        return best;
    }

    /// <summary>
    ///     Unpatches all Toolsmith compatibility patches and clears state.
    ///     Called from DivineAscensionModSystem.Dispose().
    /// </summary>
    public static void ClearSubscribers()
    {
        _harmony?.UnpatchAll("com.divineascension.toolsmith");
        _harmony = null;
        _emitter = null;
        _initialized = false;
        GrindstoneSharpeningFired.Clear();
        GrindstoneDisassemblyFired.Clear();
        WhetstoneSharpeningFired.Clear();
        WorkbenchReforgeCooldown.Clear();
        WorkbenchDisassemblyFired.Clear();
    }
}