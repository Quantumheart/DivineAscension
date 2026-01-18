using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models;
using DivineAscension.Services.Interfaces;

namespace DivineAscension.Tests.Helpers;

/// <summary>
///     Test implementation of IBlessingLoader for unit testing.
///     Allows tests to provide custom blessings without file I/O.
/// </summary>
[ExcludeFromCodeCoverage]
public class TestBlessingLoader : IBlessingLoader
{
    private readonly List<Blessing> _blessings;
    private readonly bool _shouldSucceed;

    /// <summary>
    ///     Creates a test loader with the specified blessings.
    /// </summary>
    /// <param name="blessings">The blessings to return when LoadBlessings is called</param>
    /// <param name="shouldSucceed">Whether LoadedSuccessfully should return true</param>
    public TestBlessingLoader(List<Blessing>? blessings = null, bool shouldSucceed = true)
    {
        _blessings = blessings ?? new List<Blessing>();
        _shouldSucceed = shouldSucceed && _blessings.Count > 0;
    }

    /// <inheritdoc />
    public bool LoadedSuccessfully { get; private set; }

    /// <inheritdoc />
    public int LoadedCount => _blessings.Count;

    /// <inheritdoc />
    public List<Blessing> LoadBlessings()
    {
        LoadedSuccessfully = _shouldSucceed;
        return new List<Blessing>(_blessings);
    }

    /// <summary>
    ///     Creates a test loader that simulates a failed load.
    /// </summary>
    public static TestBlessingLoader CreateFailedLoader()
    {
        return new TestBlessingLoader(new List<Blessing>(), shouldSucceed: false);
    }

    /// <summary>
    ///     Creates a test loader with sample blessings for testing.
    /// </summary>
    public static TestBlessingLoader CreateWithSampleBlessings()
    {
        var blessings = new List<Blessing>
        {
            TestFixtures.CreateTestBlessing("test_blessing_1", "Test Blessing 1"),
            TestFixtures.CreateTestBlessing("test_blessing_2", "Test Blessing 2"),
            TestFixtures.CreateTestBlessing("test_blessing_3", "Test Blessing 3")
        };

        return new TestBlessingLoader(blessings);
    }
}
