using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using DivineAscension.API.Implementation;
using DivineAscension.API.Interfaces;
using DivineAscension.Blocks;
using DivineAscension.GUI.UI.Renderers.Caravan;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network.Caravan;
using DivineAscension.Systems.Caravan;
using DivineAscension.Systems.Networking.Client;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using VSImGui;
using VSImGui.API;

namespace DivineAscension.GUI.Caravan;

/// <summary>
///     Client-side ImGui dialog for the caravan shrine trade table (#433). Opens when the
///     server sends a <see cref="TradeStateSyncPacket" /> for a session the local player is
///     seated at (triggered by right-clicking a caravan shrine), renders the "Bill of Barter"
///     ledger, and relays click intents back as offer/ready/cancel packets. UI + sync only —
///     no item movement.
/// </summary>
[ExcludeFromCodeCoverage]
public class CaravanTradeDialog : ModSystem
{
    private const float WindowWidth = 1040f;
    private const float WindowHeight = 660f;

    private readonly CaravanTradeState _state = new();

    private ICoreClientAPI? _capi;
    private DivineAscensionModSystem? _das;
    private DivineAscensionNetworkClient? _network;
    private ImGuiModSystem? _imgui;
    private IModLoaderService? _modLoader;
    private ImGuiViewportPtr _viewport;

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override double ExecuteOrder() => 1.6; // after DivineAscensionModSystem (1.0) and GuiDialog (1.5)

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        _capi = api;
        _viewport = ImGui.GetMainViewport();
        _modLoader = new ModLoaderService(api.ModLoader);

        _das = _modLoader.GetModSystem<DivineAscensionModSystem>();
        _network = _das?.NetworkClient;
        if (_network != null)
            _network.TradeStateReceived += OnTradeState;

        // No DI in block behaviors: route the shrine right-click through the network client.
        BlockBehaviorCaravanShrine.SetTradeInteractClientHandler(pos => _network?.SendOpenTradeRequest(pos));

        _imgui = _modLoader.GetModSystem<ImGuiModSystem>();
        if (_imgui != null)
        {
            _imgui.Draw += OnDraw;
            _imgui.Closed += OnClose;
        }

        api.Logger.Notification("[DivineAscension] Caravan trade dialog initialized");
    }

    private void OnTradeState(TradeStateSyncPacket packet)
    {
        if (packet.Phase == TradePhase.Closed)
        {
            _state.IsOpen = false;
            return;
        }

        _state.Apply(packet);
        if (!_state.IsOpen)
        {
            _state.IsOpen = true;
            _imgui?.Show();
        }
    }

    private CallbackGUIStatus OnDraw(float deltaSeconds)
    {
        if (!_state.IsOpen || _capi == null) return CallbackGUIStatus.Closed;

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            // Hide locally; the seat persists server-side until "Leave the table".
            _state.IsOpen = false;
            return CallbackGUIStatus.Closed;
        }

        UiScale.SyncFromGameSettings();
        DrawWindow();
        return CallbackGUIStatus.GrabMouse;
    }

    private void DrawWindow()
    {
        using var fontScale = UiScale.BeginFontScale();
        using var styleScale = UiScale.BeginStyleScale();

        var width = MathF.Min(WindowWidth, _viewport.Size.X - 64f);
        var height = MathF.Min(WindowHeight, _viewport.Size.Y - 64f);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, ColorPalette.Background);

        ImGui.SetNextWindowSize(new Vector2(width, height), ImGuiCond.Always);
        ImGui.SetNextWindowPos(new Vector2(
            _viewport.Pos.X + (_viewport.Size.X - width) / 2f,
            _viewport.Pos.Y + (_viewport.Size.Y - height) / 2f), ImGuiCond.Always);

        var flags = ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoScrollWithMouse |
                    ImGuiWindowFlags.NoSavedSettings;

        ImGui.Begin("DivineAscension Caravan Trade", flags);

        var pos = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();

        // Border to match the main codex frame.
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRect(pos, new Vector2(pos.X + size.X, pos.Y + size.Y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.BorderColor), 0, ImDrawFlags.None, 4f);

        var myUid = _capi!.World.Player.PlayerUID;
        var pack = BuildPack();

        var result = CaravanTradeRenderer.Draw(_state, myUid, pack, pos.X, pos.Y, size.X, size.Y);
        ApplyResult(result, myUid, pack);

        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(3);
    }

    private void ApplyResult(CaravanTradeRenderer.Result result, string myUid,
        IReadOnlyList<CaravanTradeRenderer.PackEntry> pack)
    {
        if (_network == null || _state.ShrinePos == null) return;

        if (result.AddPackIndex >= 0 && result.AddPackIndex < pack.Count)
        {
            var entry = pack[result.AddPackIndex];
            var offer = CloneMyOffer(myUid);
            if (offer.Count < CaravanTradeSession.MaxOfferSlots)
            {
                offer.Add(new TradeOfferSlot
                {
                    ItemCode = entry.Code,
                    DisplayName = entry.Name,
                    Quantity = entry.Quantity,
                    SlotIndex = offer.Count
                });
                _network.SendOfferUpdate(_state.ShrinePos, offer);
            }
        }
        else if (result.RemoveOfferIndex >= 0)
        {
            var offer = CloneMyOffer(myUid);
            if (result.RemoveOfferIndex < offer.Count)
            {
                offer.RemoveAt(result.RemoveOfferIndex);
                _network.SendOfferUpdate(_state.ShrinePos, offer);
            }
        }
        else if (result.SealToggleClicked)
        {
            _network.SendSetReady(_state.ShrinePos, !_state.MyReady(myUid));
        }
        else if (result.LeaveClicked)
        {
            _network.SendCancelTrade(_state.ShrinePos);
            _state.IsOpen = false;
        }
    }

    private List<TradeOfferSlot> CloneMyOffer(string myUid)
        => _state.MyOffer(myUid)
            .Select(s => new TradeOfferSlot
            {
                ItemCode = s.ItemCode,
                DisplayName = s.DisplayName,
                Quantity = s.Quantity,
                SlotIndex = s.SlotIndex
            })
            .ToList();

    /// <summary>
    ///     Snapshot the local player's hotbar + backpack as click-to-add pack entries.
    ///     Read-only — items are not moved (that is #434).
    /// </summary>
    private List<CaravanTradeRenderer.PackEntry> BuildPack()
    {
        var entries = new List<CaravanTradeRenderer.PackEntry>();
        var player = _capi?.World.Player;
        if (player?.InventoryManager == null) return entries;

        foreach (var className in new[] { GlobalConstants.hotBarInvClassName, GlobalConstants.backpackInvClassName })
        {
            var inventory = player.InventoryManager.GetOwnInventory(className);
            if (inventory == null) continue;

            foreach (var slot in inventory)
            {
                var stack = slot?.Itemstack;
                if (stack?.Collectible?.Code == null) continue;
                entries.Add(new CaravanTradeRenderer.PackEntry(
                    stack.Collectible.Code.ToString(),
                    stack.GetName(),
                    stack.StackSize));
            }
        }

        return entries;
    }

    private void OnClose()
    {
        _state.IsOpen = false;
    }

    public override void Dispose()
    {
        base.Dispose();
        if (_network != null)
            _network.TradeStateReceived -= OnTradeState;
        if (_imgui != null)
        {
            _imgui.Draw -= OnDraw;
            _imgui.Closed -= OnClose;
        }
        BlockBehaviorCaravanShrine.SetTradeInteractClientHandler(null);
    }
}
