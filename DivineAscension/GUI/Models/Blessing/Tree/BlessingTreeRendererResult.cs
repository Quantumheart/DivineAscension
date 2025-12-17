using System.Collections.Generic;
using DivineAscension.GUI.Events.Blessing;

namespace DivineAscension.GUI.Models.Blessing.Tree;

public record BlessingTreeRendererResult(
    IReadOnlyList<TreeEvent> Events,
    float RenderedHeight);