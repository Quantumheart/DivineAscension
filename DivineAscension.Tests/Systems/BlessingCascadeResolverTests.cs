using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using Xunit;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Unit tests for <see cref="BlessingCascadeResolver" /> (epic #425, slice 2 — #460).
///     Verifies orphan detection across the prerequisite graph: branch (AND) children cascade
///     when any prerequisite is stripped; capstone (OR) children survive while an alternate
///     prerequisite remains; unrelated branches stay intact; chains cascade transitively.
/// </summary>
[ExcludeFromCodeCoverage]
public class BlessingCascadeResolverTests
{
    private readonly Dictionary<string, Blessing> _graph = new();

    /// <summary>Registers a blessing. A null <paramref name="branch"/> makes it a capstone (OR prereqs).</summary>
    private Blessing Add(string id, string? branch, params string[] prereqs)
    {
        var b = new Blessing(id, id, DeityDomain.Craft)
        {
            Kind = BlessingKind.Player,
            Branch = branch,
            PrerequisiteBlessings = new List<string>(prereqs)
        };
        _graph[id] = b;
        return b;
    }

    private List<string> Resolve(string target, params string[] unlocked) =>
        BlessingCascadeResolver.Resolve(
            target,
            new HashSet<string>(unlocked),
            id => _graph.GetValueOrDefault(id));

    [Fact]
    public void Resolve_TargetNotUnlocked_ReturnsEmpty()
    {
        Add("p", null);
        Assert.Empty(Resolve("p" /* not in unlocked */));
    }

    [Fact]
    public void Resolve_LoneBlessing_ReturnsJustTarget()
    {
        Add("p", null);
        Assert.Equal(new[] { "p" }, Resolve("p", "p"));
    }

    [Fact]
    public void Resolve_BranchChild_CascadesWhenPrerequisiteStripped()
    {
        Add("p", null);
        Add("c", "branchA", "p"); // AND on [p]

        var cascade = Resolve("p", "p", "c");

        Assert.Equal(new[] { "p", "c" }, cascade);
    }

    [Fact]
    public void Resolve_LeavesUnrelatedBranchesIntact()
    {
        Add("p", null);
        Add("c", "branchA", "p");
        Add("unrelated", null); // no link to p

        var cascade = Resolve("p", "p", "c", "unrelated");

        Assert.Contains("p", cascade);
        Assert.Contains("c", cascade);
        Assert.DoesNotContain("unrelated", cascade);
    }

    [Fact]
    public void Resolve_CapstoneSurvivesWhileAlternatePrerequisiteRemains()
    {
        Add("p1", null);
        Add("p2", null);
        Add("cap", null, "p1", "p2"); // capstone OR over [p1, p2]

        // Striking p1 leaves p2 unlocked, so the OR capstone is still satisfied.
        var cascade = Resolve("p1", "p1", "p2", "cap");

        Assert.Equal(new[] { "p1" }, cascade);
    }

    [Fact]
    public void Resolve_CapstoneCascadesWhenAllItsUnlockedPrerequisitesGone()
    {
        Add("p1", null);
        Add("cap", null, "p1", "p2"); // p2 was never unlocked

        // Striking p1 leaves the capstone with no unlocked prerequisite → it cascades.
        var cascade = Resolve("p1", "p1", "cap");

        Assert.Equal(new[] { "p1", "cap" }, cascade);
    }

    [Fact]
    public void Resolve_TransitiveChain_CascadesAllDescendants()
    {
        Add("p", null);
        Add("c1", "branchA", "p");
        Add("c2", "branchA", "c1");

        var cascade = Resolve("p", "p", "c1", "c2");

        Assert.Equal(new[] { "p", "c1", "c2" }, cascade);
    }
}
