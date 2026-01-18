namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Factory for creating test service instances.
/// Provides a convenient way to set up test dependencies.
/// </summary>
public static class TestServices
{
    public static FakeEventService CreateEventService() => new();
    public static FakeWorldService CreateWorldService() => new();
    public static FakePersistenceService CreatePersistenceService() => new();
    public static SpyNetworkService CreateNetworkService() => new();
    public static SpyClientNetworkService CreateClientNetworkService() => new();

    /// <summary>
    /// Create a bundle of all common test services.
    /// This is a convenience method for tests that need multiple services.
    /// </summary>
    public static ServiceBundle CreateServiceBundle()
    {
        return new ServiceBundle(
            CreateEventService(),
            CreateWorldService(),
            CreatePersistenceService(),
            CreateNetworkService()
        );
    }
}

/// <summary>
/// Bundle of common test services for easy dependency injection in tests.
/// </summary>
public sealed record ServiceBundle(
    FakeEventService EventService,
    FakeWorldService WorldService,
    FakePersistenceService PersistenceService,
    SpyNetworkService NetworkService)
{
    public void Clear()
    {
        WorldService.Clear();
        PersistenceService.Clear();
        NetworkService.Clear();
    }
}
