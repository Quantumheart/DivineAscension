using System.Collections.Generic;
using PantheonWars.GUI.Events.Blessing;

namespace PantheonWars.GUI.Models.Blessing.Tree;

public record BlessingTreeRendererResult(
    IReadOnlyList<TreeEvent> Events,
    float RenderedHeight);