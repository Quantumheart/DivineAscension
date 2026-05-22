using System.Collections.Generic;
using DivineAscension.GUI.Events.Letters;

namespace DivineAscension.GUI.Models.Letters;

public readonly struct LettersRenderResult(
    IReadOnlyList<LettersEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<LettersEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}
