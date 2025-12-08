using System.Collections.Generic;
using PantheonWars.GUI.Events;

namespace PantheonWars.GUI.Models.Blessing.Tree;

public record BlessingTreeRendererResult(
    IReadOnlyList<BlessingTreeEvent> Events,
    float RenderedHeight);