using System.Collections.Generic;
using DivineAscension.Models;
using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.State;

public class BlessingTabState
{
    public BlessingTreeState TreeState { get; } = new();
    public BlessingInfoState InfoState { get; } = new();

    /// <summary>
    ///     Per-deity player blessing state. Outer key = deity, inner key = blessingId.
    /// </summary>
    public Dictionary<DeityDomain, Dictionary<string, BlessingNodeState>> PlayerBlessingStatesByDeity { get; } = new();

    /// <summary>
    ///     Per-deity religion blessing state. Outer key = deity, inner key = blessingId.
    /// </summary>
    public Dictionary<DeityDomain, Dictionary<string, BlessingNodeState>> ReligionBlessingStatesByDeity { get; } = new();

    /// <summary>
    ///     Currently visible deity tab in the Blessing UI. Defaults to Craft until the
    ///     handler picks a religion-aware default (patron when in a religion).
    /// </summary>
    public DeityDomain ActiveDeity { get; set; } = DeityDomain.Craft;

    /// <summary>Vertical scroll position of the Vows page (I.iii).</summary>
    public float VowsPageScrollY { get; set; }

    /// <summary>
    ///     Flattened view across every deity. Convenience for tests and lookups that don't
    ///     care which deity bucket a blessing came from. On id collisions across deities,
    ///     the first deity bucket iterated wins — collisions are not expected because
    ///     server-side blessing ids are unique across the registry.
    /// </summary>
    public IReadOnlyDictionary<string, BlessingNodeState> PlayerBlessingStates => FlattenView(PlayerBlessingStatesByDeity);

    public IReadOnlyDictionary<string, BlessingNodeState> ReligionBlessingStates => FlattenView(ReligionBlessingStatesByDeity);

    private static IReadOnlyDictionary<string, BlessingNodeState> FlattenView(
        Dictionary<DeityDomain, Dictionary<string, BlessingNodeState>> source)
    {
        var flat = new Dictionary<string, BlessingNodeState>();
        foreach (var bucket in source.Values)
            foreach (var kv in bucket)
                flat[kv.Key] = kv.Value;
        return flat;
    }

    public void Reset()
    {
        TreeState.Reset();
        InfoState.Reset();
        PlayerBlessingStatesByDeity.Clear();
        ReligionBlessingStatesByDeity.Clear();
        ActiveDeity = DeityDomain.Craft;
        VowsPageScrollY = 0f;
    }
}

public class BlessingTreeState
{
    public string? SelectedBlessingId { get; set; }
    public string? HoveringBlessingId { get; set; }
    public ScrollState PlayerScrollState { get; } = new();
    public ScrollState ReligionScrollState { get; } = new();


    public void Reset()
    {
        SelectedBlessingId = null;
        HoveringBlessingId = null;
        PlayerScrollState.Reset();
        ReligionScrollState.Reset();
    }
}

public class BlessingInfoState
{
    public void Reset()
    {
        // No state currently - info panel is display-only
    }
}
