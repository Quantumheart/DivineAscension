using System;
using System.Collections.Generic;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.Interfaces;
using DivineAscension.GUI.Models.Blessing.Tab;
using DivineAscension.Systems;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Renderers.Blessing;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Client;

namespace DivineAscension.GUI.Managers;

/// <summary>
///     Manages blessing tab state and event processing
/// </summary>
public class BlessingStateManager(ICoreClientAPI api, IUiService uiService, ISoundManager soundManager)
{
    private static readonly DeityDomain[] AllDeities =
    {
        DeityDomain.Craft, DeityDomain.Wild, DeityDomain.Conquest, DeityDomain.Harvest, DeityDomain.Stone
    };

    private readonly ICoreClientAPI _coreClientApi = api ?? throw new ArgumentNullException(nameof(api));

    private readonly ISoundManager
        _soundManager = soundManager ?? throw new ArgumentNullException(nameof(soundManager));

    private readonly IUiService _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));

    /// <summary>
    ///     Whether the viewing player founded their religion, captured from the most recent
    ///     <see cref="DrawVowsTab"/> call. Mirrors the server's founder-only gate on swearing
    ///     communal vows (#453) so a non-founder's double-click on a vow is rejected before a
    ///     confirmation dialog or request is raised — the server stays authoritative.
    /// </summary>
    internal bool IsReligionFounder { get; set; }

    public BlessingTabState State { get; } = new();

    /// <summary>
    ///     Player's maximum personal-blessing unlock slots (favor rank + prestige bonus),
    ///     synced from the server via <see cref="DivineAscension.Network.PlayerReligionDataPacket"/>.
    ///     0 means "not yet known" — the cap is not enforced client-side until the server reports
    ///     a value (the server is authoritative either way, see #444).
    /// </summary>
    public int MaxPlayerBlessingSlots { get; set; }

    /// <summary>
    ///     Fraction of favor refunded on unlearn, synced from the server's GameBalanceConfig via
    ///     <see cref="DivineAscension.Network.BlessingDataResponsePacket" /> (#460). Drives the
    ///     total-refund preview in the cascade confirm dialog. Defaults to 0.5 until synced.
    /// </summary>
    public float UnlearnRefundPercent { get; set; } = 0.5f;

    /// <summary>
    ///     True while the server's admin-opened free-respec window is active (#462). Drives the
    ///     "Free Respec" banner on the Blessings chapter; synced from
    ///     <see cref="DivineAscension.Network.BlessingDataResponsePacket" />.
    /// </summary>
    public bool FreeRespecActive { get; set; }

    /// <summary>
    ///     Count of unlocked personal (player-kind) blessings across every deity. This is the
    ///     value compared against <see cref="MaxPlayerBlessingSlots"/> for the cap.
    /// </summary>
    public int UnlockedPlayerBlessingCount
    {
        get
        {
            var count = 0;
            foreach (var bucket in State.PlayerBlessingStatesByDeity.Values)
                foreach (var node in bucket.Values)
                    if (node.IsUnlocked)
                        count++;
            return count;
        }
    }

    private Dictionary<int, string> _committedBranches = new();
    private Dictionary<int, List<string>> _lockedBranches = new();

    /// <summary>
    ///     Player blessings for the currently active deity tab. Empty dict if none loaded.
    /// </summary>
    public IReadOnlyDictionary<string, BlessingNodeState> ActivePlayerBlessings =>
        State.PlayerBlessingStatesByDeity.TryGetValue(State.ActiveDeity, out var d)
            ? d
            : EmptyStates;

    /// <summary>
    ///     Religion blessings for the currently active deity tab. Empty dict if none loaded.
    /// </summary>
    public IReadOnlyDictionary<string, BlessingNodeState> ActiveReligionBlessings =>
        State.ReligionBlessingStatesByDeity.TryGetValue(State.ActiveDeity, out var d)
            ? d
            : EmptyStates;

    private static readonly Dictionary<string, BlessingNodeState> EmptyStates = new();

    public void DrawBlessingsTab(float windowPosX, float windowPosY, float width, float contentHeight, int windowWidth,
        int windowHeight, float deltaTime, int playerFavor, int religionPrestige, DeityDomain patronDomain,
        Dictionary<DeityDomain, int>? favorRanksByDeity = null,
        Dictionary<DeityDomain, int>? totalFavorEarnedByDeity = null,
        int discipleThreshold = 500, int zealotThreshold = 2000,
        int championThreshold = 5000, int avatarThreshold = 10000)
    {
        var summaries = BuildDeitySummaries(
            patronDomain,
            favorRanksByDeity ?? new Dictionary<DeityDomain, int>(),
            totalFavorEarnedByDeity ?? new Dictionary<DeityDomain, int>(),
            discipleThreshold, zealotThreshold, championThreshold, avatarThreshold);

        var vm = new BlessingTabViewModel(
            windowPosX,
            windowPosY,
            width,
            contentHeight,
            windowWidth,
            windowHeight,
            deltaTime,
            State.TreeState.SelectedBlessingId,
            GetSelectedBlessingState(),
            ActivePlayerBlessings,
            ActiveReligionBlessings,
            State.TreeState.PlayerScrollState,
            State.TreeState.ReligionScrollState,
            playerFavor,
            religionPrestige,
            State.ActiveDeity,
            patronDomain,
            summaries,
            prestigeNextThreshold: 0,
            patronDeityName: null,
            vowsPageScrollY: 0f,
            blessingsPageScrollY: State.BlessingsPageScrollY,
            unlockedPlayerCount: UnlockedPlayerBlessingCount,
            maxBlessingSlots: MaxPlayerBlessingSlots,
            pendingUnlockState: GetPendingUnlockState(),
            pendingUnlearnState: GetPendingUnlearnState(),
            pendingUnlearnCascadeNames: GetPendingUnlearnPreview(out var unlearnRefundTotal),
            pendingUnlearnRefundTotal: unlearnRefundTotal,
            freeRespecActive: FreeRespecActive
        );

        var result = BlessingTabRenderer.DrawBlessingsTab(vm);

        ProcessBlessingTabEvents(result);
    }

    /// <summary>
    ///     I.iii — Vows of the Order. Draws the religion (communal) blessing tree
    ///     migrated off III.ii. <paramref name="isReligionFounder"/> gates double-click
    ///     unlocks of communal vows (#453); <paramref name="prestigeNextThreshold"/> drives
    ///     the right-aligned "Prestige · {N} / {M}" balance under the patron heading.
    /// </summary>
    public void DrawVowsTab(float windowPosX, float windowPosY, float width, float contentHeight, int windowWidth,
        int windowHeight, float deltaTime, int playerFavor, int religionPrestige, DeityDomain patronDomain,
        bool isReligionFounder, string? patronDeityName, int prestigeNextThreshold,
        Dictionary<DeityDomain, int>? favorRanksByDeity = null,
        Dictionary<DeityDomain, int>? totalFavorEarnedByDeity = null,
        int discipleThreshold = 500, int zealotThreshold = 2000,
        int championThreshold = 5000, int avatarThreshold = 10000)
    {
        // Capture founder status for the double-click unlock gate (#453).
        IsReligionFounder = isReligionFounder;

        var summaries = BuildDeitySummaries(
            patronDomain,
            favorRanksByDeity ?? new Dictionary<DeityDomain, int>(),
            totalFavorEarnedByDeity ?? new Dictionary<DeityDomain, int>(),
            discipleThreshold, zealotThreshold, championThreshold, avatarThreshold);

        var vm = new BlessingTabViewModel(
            windowPosX,
            windowPosY,
            width,
            contentHeight,
            windowWidth,
            windowHeight,
            deltaTime,
            State.TreeState.SelectedBlessingId,
            GetSelectedBlessingState(),
            ActivePlayerBlessings,
            ActiveReligionBlessings,
            State.TreeState.PlayerScrollState,
            State.TreeState.ReligionScrollState,
            playerFavor,
            religionPrestige,
            State.ActiveDeity,
            patronDomain,
            summaries,
            prestigeNextThreshold,
            patronDeityName,
            State.VowsPageScrollY,
            blessingsPageScrollY: 0f,
            pendingUnlockState: GetPendingUnlockState()
        );

        var result = BlessingVowsTabRenderer.Draw(vm);

        ProcessBlessingTabEvents(result);
    }

    internal void ProcessBlessingTabEvents(BlessingTabRenderResult result)
    {
        State.TreeState.HoveringBlessingId = result.HoveringBlessingId;

        // The unlock confirmation behaves as a modal (#453): while it's open, ignore every
        // background interaction (scroll, deity switch, tree select/double-click) — clicks
        // behind the dim backdrop fall through in immediate mode, so we drop their effects
        // here and act only on the dialog's own confirm/cancel events below.
        var modalOpen = State.PendingUnlockBlessingId != null || State.PendingUnlearnBlessingId != null;

        if (!modalOpen)
        {
            if (result.RequestedVowsScrollY is { } vowsScrollY)
                State.VowsPageScrollY = vowsScrollY;

            if (result.RequestedPageScrollY is { } pageScrollY)
                State.BlessingsPageScrollY = pageScrollY;

            if (result.RequestedActiveDeity is { } newActive && newActive != State.ActiveDeity)
            {
                State.ActiveDeity = newActive;
                State.TreeState.SelectedBlessingId = null;
                State.TreeState.PlayerScrollState.Reset();
                State.TreeState.ReligionScrollState.Reset();
                _soundManager.PlayClick();
            }

            foreach (var ev in result.TreeEvents)
                switch (ev)
                {
                    case TreeEvent.Selected e:
                        State.TreeState.SelectedBlessingId = e.BlessingId;
                        _soundManager.PlayClick();
                        break;

                    case TreeEvent.DoubleClicked e:
                        State.TreeState.SelectedBlessingId = e.BlessingId;
                        HandleUnlockClicked();
                        break;

                    case TreeEvent.Hovered:
                        break;

                    case TreeEvent.PlayerTreeScrollChanged e:
                        State.TreeState.PlayerScrollState.X = e.ScrollX;
                        State.TreeState.PlayerScrollState.Y = e.ScrollY;
                        break;

                    case TreeEvent.ReligionTreeScrollChanged e:
                        State.TreeState.ReligionScrollState.X = e.ScrollX;
                        State.TreeState.ReligionScrollState.Y = e.ScrollY;
                        break;
                }
        }

        foreach (var ev in result.ActionsEvents)
            switch (ev)
            {
                case ActionsEvent.UnlockConfirmed:
                    HandleUnlockConfirmed();
                    break;

                case ActionsEvent.UnlockCanceled:
                    HandleUnlockCanceled();
                    break;

                case ActionsEvent.UnlearnConfirmed:
                    HandleUnlearnConfirmed();
                    break;

                case ActionsEvent.UnlearnCanceled:
                    HandleUnlearnCanceled();
                    break;
            }
    }


    /// <summary>
    ///     Validates the selected blessing and, if eligible, opens the unlock confirmation
    ///     dialog (#453) instead of dispatching immediately. The favor/prestige spend is only
    ///     committed once the player confirms via <see cref="HandleUnlockConfirmed"/>.
    /// </summary>
    private void HandleUnlockClicked()
    {
        if (State.TreeState.SelectedBlessingId == null) return;
        var selectedState = GetBlessingState(State.TreeState.SelectedBlessingId);
        if (selectedState == null) return;

        // Double-clicking an owned personal blessing unlearns it (epic #425, slice 1 — #459).
        // Religion vows aren't unlearnable here; the favor refund is the only cost (no cooldown).
        // The confirm dialog with a kill list arrives with the cascade slice — dispatch directly.
        if (selectedState.IsUnlocked)
        {
            if (selectedState.Blessing.Kind == BlessingKind.Player
                && !string.IsNullOrEmpty(selectedState.Blessing.BlessingId))
            {
                // Open the unlearn confirmation dialog instead of dispatching immediately —
                // mirrors the unlock confirm flow (#453); request is sent on confirm (#459).
                _soundManager.PlayClick();
                State.PendingUnlearnBlessingId = selectedState.Blessing.BlessingId;
            }
            return;
        }

        if (!selectedState.CanUnlock)
        {
            if (selectedState.BlockedByCap)
                _coreClientApi.ShowChatMessage(
                    LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_TOOLTIP_SLOT_CAP));
            _soundManager.PlayError();
            return;
        }

        if (string.IsNullOrEmpty(selectedState.Blessing.BlessingId))
        {
            _coreClientApi.ShowChatMessage("Error: Invalid blessing ID");
            return;
        }

        // Communal vows are founder-only — mirror the server gate so non-founders never
        // reach the confirmation dialog or spend prestige (#453).
        if (selectedState.Blessing.Kind == BlessingKind.Religion && !IsReligionFounder)
        {
            _coreClientApi.ShowChatMessage(
                LocalizationService.Instance.Get(LocalizationKeys.SIDEBAR_DISABLED_SWEAR_FOUNDER_ONLY));
            _soundManager.PlayError();
            return;
        }

        _soundManager.PlayClick();

        State.PendingUnlockBlessingId = selectedState.Blessing.BlessingId;
    }

    /// <summary>
    ///     Dispatches the unlock request for the blessing awaiting confirmation (#453).
    ///     Re-validates eligibility before sending — the server remains authoritative.
    /// </summary>
    private void HandleUnlockConfirmed()
    {
        var pendingId = State.PendingUnlockBlessingId;
        State.PendingUnlockBlessingId = null;
        if (string.IsNullOrEmpty(pendingId)) return;

        var state = GetBlessingState(pendingId);
        if (state == null || state.IsUnlocked || !state.CanUnlock) return;

        _soundManager.PlayClick();
        _uiService.RequestBlessingUnlock(pendingId);
    }

    /// <summary>Dismisses the unlock confirmation dialog (#453) with no side effects.</summary>
    private void HandleUnlockCanceled()
    {
        State.PendingUnlockBlessingId = null;
        _soundManager.PlayClick();
    }

    private BlessingNodeState? GetPendingUnlockState()
    {
        return string.IsNullOrEmpty(State.PendingUnlockBlessingId)
            ? null
            : GetBlessingState(State.PendingUnlockBlessingId);
    }

    /// <summary>
    ///     Dispatches the unlearn request for the blessing awaiting confirmation (#459).
    ///     Re-validates ownership before sending — the server remains authoritative.
    /// </summary>
    private void HandleUnlearnConfirmed()
    {
        var pendingId = State.PendingUnlearnBlessingId;
        State.PendingUnlearnBlessingId = null;
        if (string.IsNullOrEmpty(pendingId)) return;

        var state = GetBlessingState(pendingId);
        if (state == null || !state.IsUnlocked || state.Blessing.Kind != BlessingKind.Player) return;

        _soundManager.PlayClick();
        _uiService.RequestBlessingUnlearn(pendingId);
    }

    /// <summary>Dismisses the unlearn confirmation dialog (#459) with no side effects.</summary>
    private void HandleUnlearnCanceled()
    {
        State.PendingUnlearnBlessingId = null;
        _soundManager.PlayClick();
    }

    private BlessingNodeState? GetPendingUnlearnState()
    {
        return string.IsNullOrEmpty(State.PendingUnlearnBlessingId)
            ? null
            : GetBlessingState(State.PendingUnlearnBlessingId);
    }

    /// <summary>
    ///     Previews the unlearn cascade for the pending blessing (#460): the ordered kill-list
    ///     names (target first) and the estimated total favor refund. Returns null when no unlearn
    ///     dialog is open. Uses the same <see cref="BlessingCascadeResolver" /> as the server, so the
    ///     preview matches the authoritative strip; the refund estimate uses the synced percent.
    /// </summary>
    private IReadOnlyList<string>? GetPendingUnlearnPreview(out int totalRefund)
    {
        totalRefund = 0;
        if (string.IsNullOrEmpty(State.PendingUnlearnBlessingId))
            return null;

        var nodesById = new Dictionary<string, BlessingNodeState>();
        foreach (var bucket in State.PlayerBlessingStatesByDeity.Values)
            foreach (var kv in bucket)
                nodesById[kv.Key] = kv.Value;

        var unlocked = new HashSet<string>();
        foreach (var (id, node) in nodesById)
            if (node.IsUnlocked)
                unlocked.Add(id);

        var cascade = BlessingCascadeResolver.Resolve(
            State.PendingUnlearnBlessingId,
            unlocked,
            id => nodesById.TryGetValue(id, out var n) ? n.Blessing : null);

        var names = new List<string>(cascade.Count);
        foreach (var id in cascade)
        {
            if (!nodesById.TryGetValue(id, out var node)) continue;
            names.Add(node.Blessing.Name);
            var paidCost = (int)(node.Blessing.Cost * node.NonPatronCostMultiplier);
            totalRefund += (int)(paidCost * UnlearnRefundPercent);
        }

        return names;
    }

    private BlessingNodeState? GetBlessingState(string blessingId)
    {
        foreach (var dict in State.PlayerBlessingStatesByDeity.Values)
            if (dict.TryGetValue(blessingId, out var s)) return s;
        foreach (var dict in State.ReligionBlessingStatesByDeity.Values)
            if (dict.TryGetValue(blessingId, out var s)) return s;
        return null;
    }

    public BlessingNodeState? GetSelectedBlessingState()
    {
        if (string.IsNullOrEmpty(State.TreeState.SelectedBlessingId)) return null;
        return GetBlessingState(State.TreeState.SelectedBlessingId);
    }

    /// <summary>
    ///     Load blessings for all five deities. `playerBlessings` and `religionBlessings`
    ///     should already contain entries for every deity — server populates them in
    ///     <see cref="DivineAscension.Network.BlessingDataResponsePacket"/> after #240.
    /// </summary>
    public void LoadBlessingStates(List<Blessing> playerBlessings, List<Blessing> religionBlessings)
    {
        State.PlayerBlessingStatesByDeity.Clear();
        State.ReligionBlessingStatesByDeity.Clear();

        foreach (var domain in AllDeities)
        {
            State.PlayerBlessingStatesByDeity[domain] = new Dictionary<string, BlessingNodeState>();
            State.ReligionBlessingStatesByDeity[domain] = new Dictionary<string, BlessingNodeState>();
        }

        foreach (var blessing in playerBlessings)
        {
            if (!State.PlayerBlessingStatesByDeity.TryGetValue(blessing.Domain, out var bucket))
            {
                bucket = new Dictionary<string, BlessingNodeState>();
                State.PlayerBlessingStatesByDeity[blessing.Domain] = bucket;
            }
            bucket[blessing.BlessingId] = new BlessingNodeState(blessing);
        }

        foreach (var blessing in religionBlessings)
        {
            if (!State.ReligionBlessingStatesByDeity.TryGetValue(blessing.Domain, out var bucket))
            {
                bucket = new Dictionary<string, BlessingNodeState>();
                State.ReligionBlessingStatesByDeity[blessing.Domain] = bucket;
            }
            bucket[blessing.BlessingId] = new BlessingNodeState(blessing);
        }
    }

    public void SetBlessingUnlocked(string blessingId, bool unlocked)
    {
        var state = GetBlessingState(blessingId);
        if (state != null)
        {
            state.IsUnlocked = unlocked;
            state.UpdateVisualState();
        }
    }

    public void RefreshAllBlessingStates(
        Dictionary<DeityDomain, int> favorRanksByDeity,
        int currentPrestigeRank,
        DeityDomain patronDomain)
    {
        // Cap gate (#446): once the player fills their personal unlock slots, every
        // otherwise-eligible player blessing flips to blocked-by-cap. 0 = not yet synced,
        // so the cap stays open until the server reports a value (server is authoritative).
        var atCap = MaxPlayerBlessingSlots > 0 && UnlockedPlayerBlessingCount >= MaxPlayerBlessingSlots;

        foreach (var bucket in State.PlayerBlessingStatesByDeity.Values)
            foreach (var state in bucket.Values)
            {
                var branchLocked = IsBranchLocked(state.Blessing.Domain, state.Blessing.Branch);
                state.IsBranchLocked = branchLocked;
                state.LockedByBranch = branchLocked ? GetCommittedBranch(state.Blessing.Domain) : null;
                state.NonPatronCostMultiplier = state.Blessing.Domain == patronDomain ? 1.0f : 1.5f;
                var meetsRequirements = CanUnlockBlessing(state, favorRanksByDeity, currentPrestigeRank, patronDomain);
                state.BlockedByCap = meetsRequirements && atCap;
                state.CanUnlock = meetsRequirements && !atCap;
                state.UpdateVisualState();
            }

        foreach (var bucket in State.ReligionBlessingStatesByDeity.Values)
            foreach (var state in bucket.Values)
            {
                state.IsBranchLocked = false;
                state.LockedByBranch = null;
                state.BlockedByCap = false;
                state.NonPatronCostMultiplier = state.Blessing.Domain == patronDomain ? 1.0f : 1.5f;
                state.CanUnlock = CanUnlockBlessing(state, favorRanksByDeity, currentPrestigeRank, patronDomain);
                state.UpdateVisualState();
            }
    }

    private bool CanUnlockBlessing(
        BlessingNodeState state,
        Dictionary<DeityDomain, int> favorRanksByDeity,
        int currentPrestigeRank,
        DeityDomain patronDomain)
    {
        if (state.IsUnlocked) return false;
        if (state.IsBranchLocked) return false;
        if (state.Blessing.RequiresPatron && patronDomain != state.Blessing.Domain) return false;

        if (state.Blessing.PrerequisiteBlessings is { Count: > 0 })
        {
            var isCapstone = string.IsNullOrEmpty(state.Blessing.Branch);

            if (isCapstone)
            {
                var anyUnlocked = false;
                foreach (var prereqId in state.Blessing.PrerequisiteBlessings)
                {
                    var prereqState = GetBlessingState(prereqId);
                    if (prereqState != null && prereqState.IsUnlocked)
                    {
                        anyUnlocked = true;
                        break;
                    }
                }
                if (!anyUnlocked) return false;
            }
            else
            {
                foreach (var prereqId in state.Blessing.PrerequisiteBlessings)
                {
                    var prereqState = GetBlessingState(prereqId);
                    if (prereqState == null || !prereqState.IsUnlocked) return false;
                }
            }
        }

        if (state.Blessing.Kind == BlessingKind.Player)
        {
            var domainRank = favorRanksByDeity.GetValueOrDefault(state.Blessing.Domain);
            if (state.Blessing.RequiredFavorRank > domainRank) return false;
        }
        else if (state.Blessing.Kind == BlessingKind.Religion)
        {
            if (state.Blessing.RequiredPrestigeRank > currentPrestigeRank) return false;
        }

        return true;
    }

    private List<DeityBlessingSummary> BuildDeitySummaries(
        DeityDomain patronDomain,
        Dictionary<DeityDomain, int> favorRanksByDeity,
        Dictionary<DeityDomain, int> totalFavorEarnedByDeity,
        int discipleThreshold, int zealotThreshold, int championThreshold, int avatarThreshold)
    {
        var list = new List<DeityBlessingSummary>(AllDeities.Length);
        foreach (var domain in AllDeities)
        {
            var rank = favorRanksByDeity.GetValueOrDefault(domain);
            var totalFavor = totalFavorEarnedByDeity.GetValueOrDefault(domain);
            var requiredForNext = RankRequirements.GetRequiredFavorForNextRank(
                rank, discipleThreshold, zealotThreshold, championThreshold, avatarThreshold);
            var playerBucket = State.PlayerBlessingStatesByDeity.GetValueOrDefault(domain);
            var religionBucket = State.ReligionBlessingStatesByDeity.GetValueOrDefault(domain);
            var unlockedPlayer = 0;
            var totalPlayer = 0;
            if (playerBucket != null)
                foreach (var s in playerBucket.Values)
                {
                    totalPlayer++;
                    if (s.IsUnlocked) unlockedPlayer++;
                }
            var unlockedReligion = 0;
            var totalReligion = 0;
            if (religionBucket != null)
                foreach (var s in religionBucket.Values)
                {
                    totalReligion++;
                    if (s.IsUnlocked) unlockedReligion++;
                }

            list.Add(new DeityBlessingSummary(
                Domain: domain,
                FavorRank: rank,
                TotalFavorEarned: totalFavor,
                FavorRequiredForNext: requiredForNext,
                IsMaxRank: rank >= 4,
                UnlockedPlayer: unlockedPlayer,
                TotalPlayer: totalPlayer,
                UnlockedReligion: unlockedReligion,
                TotalReligion: totalReligion,
                IsPatron: patronDomain != DeityDomain.None && domain == patronDomain,
                IsActive: domain == State.ActiveDeity));
        }
        return list;
    }

    public void SetActiveDeity(DeityDomain domain)
    {
        if (State.ActiveDeity == domain) return;
        State.ActiveDeity = domain;
        State.TreeState.SelectedBlessingId = null;
        State.TreeState.PlayerScrollState.Reset();
        State.TreeState.ReligionScrollState.Reset();
    }

    public void ClearSelection()
    {
        State.TreeState.SelectedBlessingId = null;
    }


    public void SelectBlessing(string blessingId)
    {
        State.TreeState.SelectedBlessingId = blessingId;
    }

    public void SetBranchState(Dictionary<int, string> committedBranches, Dictionary<int, List<string>> lockedBranches)
    {
        _committedBranches = committedBranches ?? new Dictionary<int, string>();
        _lockedBranches = lockedBranches ?? new Dictionary<int, List<string>>();
    }

    private bool IsBranchLocked(DeityDomain domain, string? branch)
    {
        if (string.IsNullOrEmpty(branch))
            return false;

        var domainKey = (int)domain;
        return _lockedBranches.TryGetValue(domainKey, out var locked) && locked.Contains(branch);
    }

    private string? GetCommittedBranch(DeityDomain domain)
    {
        return _committedBranches.GetValueOrDefault((int)domain);
    }
}
