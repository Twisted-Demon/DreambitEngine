using Dreambit;
using Dreambit.ECS;
using Dreambit.UI;
using DreambitEngine.AssetBaker.Pipeline;

namespace Dreambit.Editor.Tests;

public sealed class UiAssetLoadingTests : IDisposable
{
    private readonly AssetContentMode _originalContentMode = Resources.ContentMode;
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "Dreambit.Editor.UiAssetLoadingTests",
        Guid.NewGuid().ToString("N"));

    public UiAssetLoadingTests()
    {
        WriteUiAssets();
    }

    [Fact]
    public void UiFrameLoadsLayoutAndComponentsFromBakedBlobs()
    {
        var assets = Path.Combine(_root, "Assets");
        var blobs = Path.Combine(_root, "Blobs");
        new AssetBakePipeline().BakeBlobs(new AssetBlobBakeRequest(
            assets,
            blobs,
            RebuildAll: true));

        Resources.SetBlobContentSource(blobs);

        AssertFrameLoadsBakedUi();
    }

    [Fact]
    public void UiFrameLoadsLayoutAndComponentsFromPak()
    {
        var assets = Path.Combine(_root, "Assets");
        var content = Path.Combine(_root, "Content");
        var pak = Path.Combine(content, "content.pak");
        new AssetBakePipeline().BakePak(new AssetBakeRequest(
            assets,
            pak,
            RebuildAll: true));

        Resources.SetContentSource(content);
        Resources.ContentMode = AssetContentMode.Pak;

        AssertFrameLoadsBakedUi();
    }

    public void Dispose()
    {
        Resources.ResetContentSource();
        Resources.ContentMode = _originalContentMode;

        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private void AssertFrameLoadsBakedUi()
    {
        var frame = new UiFrame().WithLayout("Ui/main.xml");

        Assert.Equal("Ui/main.xml", frame.LayoutPath);
        Assert.IsType<UiButton>(frame.Layout.Find("primary.button"));
        Assert.IsType<UiText>(frame.Layout.Find("primary.label"));
        Assert.IsType<UiButton>(frame.Layout.Find("secondary.button"));

        var detached = frame.CreateComponent(
            "Ui/components/menu-button.xml",
            "dynamic");
        Assert.Equal("dynamic.button", detached.Id);
        Assert.Equal("dynamic.label", Assert.Single(detached.Children).Id);
    }

    private void WriteUiAssets()
    {
        var uiDirectory = Path.Combine(_root, "Assets", "Ui");
        var componentDirectory = Path.Combine(uiDirectory, "components");
        Directory.CreateDirectory(componentDirectory);

        File.WriteAllText(
            Path.Combine(uiDirectory, "main.xml"),
            """
            <Ui>
              <Ui.Components>
                <Component name="MenuButton" source="components/menu-button.xml" />
              </Ui.Components>
              <Panel id="surface">
                <MenuButton id-prefix="primary" />
                <Include source="~/Ui/components/menu-button.xml" id-prefix="secondary" />
              </Panel>
            </Ui>
            """);

        File.WriteAllText(
            Path.Combine(componentDirectory, "menu-button.xml"),
            """
            <UiComponent>
              <Button id="button">
                <Text id="label" text="Play" />
              </Button>
            </UiComponent>
            """);
    }
}
