namespace DivineAscension.GUI.UI.Layout;

/// <summary>
///     Immutable rectangle in screen-space pixels. Used to flow layout regions
///     down through renderer chains without leaking raw <c>ImGui.GetWindowPos()</c>
///     calls into leaf renderers.
/// </summary>
public readonly record struct UiRect(float X, float Y, float W, float H)
{
    public float Right => X + W;
    public float Bottom => Y + H;

    /// <summary>
    ///     Cut a fixed-width slice off the left. Returns the slice and the remainder
    ///     (separated by <paramref name="gap" /> pixels). If <paramref name="width" />
    ///     exceeds available width, the slice clamps to the rect and the remainder
    ///     becomes a zero-width rect on the right edge.
    /// </summary>
    public (UiRect Left, UiRect Remainder) SplitLeft(float width, float gap = 0f)
    {
        var clampedGap = gap < 0f ? 0f : gap;
        var sliceW = width < 0f ? 0f : (width > W ? W : width);
        var left = new UiRect(X, Y, sliceW, H);
        var remainderX = X + sliceW + clampedGap;
        var remainderW = Right - remainderX;
        if (remainderW < 0f) remainderW = 0f;
        return (left, new UiRect(remainderX, Y, remainderW, H));
    }

    /// <summary>
    ///     Cut a fixed-width slice off the right. Returns the remainder and the slice
    ///     (separated by <paramref name="gap" /> pixels).
    /// </summary>
    public (UiRect Remainder, UiRect Right) SplitRight(float width, float gap = 0f)
    {
        var clampedGap = gap < 0f ? 0f : gap;
        var sliceW = width < 0f ? 0f : (width > W ? W : width);
        var right = new UiRect(Right - sliceW, Y, sliceW, H);
        var remainderW = W - sliceW - clampedGap;
        if (remainderW < 0f) remainderW = 0f;
        return (new UiRect(X, Y, remainderW, H), right);
    }

    /// <summary>
    ///     Shrink the rect inward by <paramref name="px" /> pixels on every side.
    /// </summary>
    public UiRect Inset(float px)
    {
        var inset = px < 0f ? 0f : px;
        var newW = W - inset * 2f;
        var newH = H - inset * 2f;
        if (newW < 0f) newW = 0f;
        if (newH < 0f) newH = 0f;
        return new UiRect(X + inset, Y + inset, newW, newH);
    }

    /// <summary>
    ///     Trim <paramref name="top" /> pixels off the top and <paramref name="bottom" />
    ///     pixels off the bottom.
    /// </summary>
    public UiRect Cut(float top, float bottom)
    {
        var t = top < 0f ? 0f : top;
        var b = bottom < 0f ? 0f : bottom;
        var newH = H - t - b;
        if (newH < 0f) newH = 0f;
        return new UiRect(X, Y + t, W, newH);
    }
}
