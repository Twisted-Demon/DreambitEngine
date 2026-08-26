using Dreambit;
using Dreambit.Networking.Scenes;
using Xunit;

namespace DreambitEngine.Networking.Tests;

public sealed class NetworkSceneCatalogTests
{
    [Fact]
    public void StableKeysResolveFactoriesAndRejectDuplicates()
    {
        var catalog = new NetworkSceneCatalog();
        catalog.Register("arena", () => new CatalogScene());

        using var scene = catalog.Create("arena");

        Assert.IsType<CatalogScene>(scene);
        Assert.Throws<InvalidOperationException>(() =>
            catalog.Register("arena", () => new CatalogScene()));
    }

    [Fact]
    public void RegistrationsAreFrozenForAnActiveSession()
    {
        var catalog = new NetworkSceneCatalog();
        catalog.Register("arena", () => new CatalogScene());

        catalog.Freeze();

        Assert.Throws<InvalidOperationException>(() =>
            catalog.Register("lobby", () => new CatalogScene()));
    }

    private sealed class CatalogScene : Scene
    {
        internal override void InitializeInternals()
        {
        }
    }
}
