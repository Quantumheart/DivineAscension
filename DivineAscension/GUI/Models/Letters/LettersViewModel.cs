using System.Collections.Generic;

namespace DivineAscension.GUI.Models.Letters;

/// <summary>
///     Immutable view model for the shared
///     <see cref="DivineAscension.GUI.UI.Renderers.Components.LettersRenderer" />.
///     Adapters fill the chrome strings (title, intro, closing line, button
///     labels, loading text) so the same renderer paints both the religion
///     Letters chapter (I.v) and the civilization Letters chapter (II.iii).
/// </summary>
public readonly struct LettersViewModel(
    IReadOnlyList<LetterEntry> letters,
    string title,
    string intro,
    string closingLine,
    string acceptLabel,
    string refuseLabel,
    string loadingText,
    bool isLoading,
    float scrollY,
    float x,
    float y,
    float width,
    float height)
{
    public IReadOnlyList<LetterEntry> Letters { get; } = letters;
    public string Title { get; } = title;
    public string Intro { get; } = intro;
    public string ClosingLine { get; } = closingLine;
    public string AcceptLabel { get; } = acceptLabel;
    public string RefuseLabel { get; } = refuseLabel;
    public string LoadingText { get; } = loadingText;
    public bool IsLoading { get; } = isLoading;
    public float ScrollY { get; } = scrollY;

    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;

    public bool HasLetters => Letters is { Count: > 0 };
}
