using Dreambit;
using Dreambit.ECS;
using Dreambit.UI;
using DreambitEngine.AssetBaker.Pipeline;
using DreambitEngine.AssetBaker.Pipeline.Docs;
using Newtonsoft.Json.Linq;
using System.Text.Json;

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

    [Fact]
    public void UiFrameLoadsStylesheetsFromLooseBakedContent()
    {
        var assets = Path.Combine(_root, "Assets");
        var content = Path.Combine(_root, "LooseContent");
        BakeLooseUi(assets, content);

        Resources.SetContentSource(content);
        Resources.ContentMode = AssetContentMode.LooseFiles;

        AssertFrameLoadsBakedUi();
    }

    [Fact]
    public void RuntimeComponentCanBeStyledBeforeALayoutExists()
    {
        var assets = Path.Combine(_root, "Assets");
        var blobs = Path.Combine(_root, "PreLayoutBlobs");
        new AssetBakePipeline().BakeBlobs(new AssetBlobBakeRequest(
            assets,
            blobs,
            RebuildAll: true));
        Resources.SetBlobContentSource(blobs);

        var frame = new UiFrame().WithCss("Ui/master.css");
        var component = frame.CreateComponent(
            "Ui/components/menu-button.xml",
            "early");
        var label = Assert.IsType<UiText>(Assert.Single(component.Children));

        Assert.Null(component.Parent);
        Assert.Equal(11f, label.X.Value);
        Assert.Equal(30f, label.Width.Value);
    }

    [Fact]
    public void UiFramePathChangesAreTransactionalAndOrderIndependent()
    {
        var assets = Path.Combine(_root, "Assets");
        var blobs = Path.Combine(_root, "TransactionalBlobs");
        new AssetBakePipeline().BakeBlobs(new AssetBlobBakeRequest(
            assets,
            blobs,
            RebuildAll: true));
        Resources.SetBlobContentSource(blobs);

        var frame = new UiFrame()
            .WithLayout("Ui/main.xml")
            .WithCss("Ui/master.css");
        var styledLayout = frame.Layout;
        Assert.Equal(11f, Assert.IsType<UiText>(frame.Layout.Find("host")).X.Value);

        Assert.Throws<FileNotFoundException>(() => frame.CssPath = "Ui/missing.css");
        Assert.Same(styledLayout, frame.Layout);
        Assert.Equal("Ui/main.xml", frame.LayoutPath);
        Assert.Equal("Ui/master.css", frame.CssPath);

        Assert.ThrowsAny<Exception>(() => frame.LayoutPath = "Ui/missing.xml");
        Assert.Same(styledLayout, frame.Layout);
        Assert.Equal("Ui/main.xml", frame.LayoutPath);
        Assert.Equal("Ui/master.css", frame.CssPath);

        frame.CssPath = null!;
        Assert.Null(frame.CssPath);
        Assert.Equal(0f, Assert.IsType<UiText>(frame.Layout.Find("host")).X.Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BlueprintPropertiesLoadLayoutAndCssInEitherOrder(bool cssFirst)
    {
        var assets = Path.Combine(_root, "Assets");
        var blobs = Path.Combine(_root, "BlueprintBlobs");
        new AssetBakePipeline().BakeBlobs(new AssetBlobBakeRequest(
            assets,
            blobs,
            RebuildAll: true));
        Resources.SetBlobContentSource(blobs);

        var properties = new Dictionary<string, JToken>();
        if (cssFirst)
            properties.Add("CssPath", JValue.CreateString("Ui/master.css"));
        properties.Add("LayoutPath", JValue.CreateString("Ui/main.xml"));
        if (!cssFirst)
            properties.Add("CssPath", JValue.CreateString("Ui/master.css"));
        var componentBlueprint = new ComponentBlueprint
        {
            Type = nameof(UiFrame),
            Properties = properties
        };
        var root = new EntityBlueprint { Name = "UI" };
        var frame = new UiFrame();

        BlueprintResolver.ResolveComponent(
            componentBlueprint,
            new BlueprintSpawnContext(root),
            frame);

        Assert.Equal("Ui/main.xml", frame.LayoutPath);
        Assert.Equal("Ui/master.css", frame.CssPath);
        Assert.Equal(11f, Assert.IsType<UiText>(frame.Layout.Find("host")).X.Value);
    }

    [Fact]
    public void BlueprintWithoutCssPathKeepsLegacyBehavior()
    {
        var assets = Path.Combine(_root, "Assets");
        var blobs = Path.Combine(_root, "LegacyBlueprintBlobs");
        new AssetBakePipeline().BakeBlobs(new AssetBlobBakeRequest(
            assets,
            blobs,
            RebuildAll: true));
        Resources.SetBlobContentSource(blobs);
        var componentBlueprint = new ComponentBlueprint
        {
            Type = nameof(UiFrame),
            Properties = new Dictionary<string, JToken>
            {
                ["LayoutPath"] = JValue.CreateString("Ui/main.xml")
            }
        };
        var frame = new UiFrame();

        BlueprintResolver.ResolveComponent(
            componentBlueprint,
            new BlueprintSpawnContext(new EntityBlueprint { Name = "Legacy UI" }),
            frame);

        Assert.Null(frame.CssPath);
        Assert.NotNull(frame.Layout);
        Assert.Equal(0f, Assert.IsType<UiText>(frame.Layout.Find("host")).X.Value);
    }

    [Fact]
    public void CorruptSiblingBlobIsNotTreatedAsAnOptionalMiss()
    {
        var assets = Path.Combine(_root, "Assets");
        var blobs = Path.Combine(_root, "CorruptBlobs");
        new AssetBakePipeline().BakeBlobs(new AssetBlobBakeRequest(
            assets,
            blobs,
            RebuildAll: true));
        var manifest = JsonSerializer.Deserialize<BlobContentManifest>(
            File.ReadAllText(Path.Combine(blobs, BlobContentManifest.FileName)),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var stylesheet = Assert.Single(
            manifest.Assets,
            entry => entry.Path.Equals("ui/main.cssb", StringComparison.OrdinalIgnoreCase));
        File.Delete(Path.Combine(blobs, stylesheet.Blob.Replace('/', Path.DirectorySeparatorChar)));
        Resources.SetBlobContentSource(blobs);

        Assert.Throws<FileNotFoundException>(() => UiLoader.LoadFromAsset("Ui/main.xml"));
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
        var frame = new UiFrame()
            .WithCss("Ui/master.css")
            .WithLayout("Ui/main.xml");

        Assert.Equal("Ui/main.xml", frame.LayoutPath);
        Assert.Equal("Ui/master.css", frame.CssPath);
        var primaryButton = Assert.IsType<UiButton>(frame.Layout.Find("primary.button"));
        var primaryLabel = Assert.IsType<UiText>(frame.Layout.Find("primary.label"));
        var secondaryButton = Assert.IsType<UiButton>(frame.Layout.Find("secondary.button"));
        var secondaryLabel = Assert.IsType<UiText>(frame.Layout.Find("secondary.label"));
        var host = Assert.IsType<UiText>(frame.Layout.Find("host"));
        Assert.Equal(11f, host.X.Value);
        Assert.Equal(20f, host.Width.Value);
        Assert.Equal(30f, primaryLabel.Width.Value);
        Assert.Equal(30f, secondaryLabel.Width.Value);
        Assert.Equal(60f, primaryButton.Height.Value);
        Assert.Equal(0f, secondaryButton.Width.Value);
        Assert.Equal(["component", "instance"], primaryButton.StyleClasses);

        var plainLayout = UiLoader.LoadFromAsset("Ui/plain");
        Assert.IsType<UiText>(plainLayout.Find("plain"));

        var detached = frame.CreateComponent(
            "Ui/components/menu-button.xml",
            "dynamic");
        Assert.Equal("dynamic.button", detached.Id);
        var detachedLabel = Assert.IsType<UiText>(Assert.Single(detached.Children));
        Assert.Equal("dynamic.label", detachedLabel.Id);
        Assert.Equal(30f, detachedLabel.Width.Value);
        Assert.Null(detached.Parent);
        detachedLabel.Width = UiLength.Pixels(99);
        frame.Layout.Root.AddChild(detached);
        Assert.Same(frame.Layout.Root, detached.Parent);
        Assert.Equal(99f, detachedLabel.Width.Value);

        var themed = frame.CreateComponent(
            "Ui/components/menu-button.xml",
            "themed",
            "Ui/runtime-theme.css");
        var themedLabel = Assert.IsType<UiText>(Assert.Single(themed.Children));
        Assert.Equal(40f, themedLabel.Width.Value);

        var extensionless = frame.CreateComponent(
            "Ui/components/menu-button",
            "extensionless");
        Assert.Equal(
            30f,
            Assert.IsType<UiText>(Assert.Single(extensionless.Children)).Width.Value);
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
                <MenuButton id-prefix="primary" class="instance" />
                <Include source="~/Ui/components/menu-button.xml" id-prefix="secondary" width="0" />
                <Text id="host" />
              </Panel>
            </Ui>
            """);

        File.WriteAllText(
            Path.Combine(componentDirectory, "menu-button.xml"),
            """
            <UiComponent>
              <Button id="button" class="component" height="60">
                <Text id="label" class="high-specificity" text="Play" />
              </Button>
            </UiComponent>
            """);

        File.WriteAllText(
            Path.Combine(uiDirectory, "master.css"),
            "Text { x: 11px; }");
        File.WriteAllText(
            Path.Combine(uiDirectory, "main.css"),
            "Text { width: 20px; } Text.high-specificity { width: 25px; }");
        File.WriteAllText(
            Path.Combine(componentDirectory, "menu-button.css"),
            "Text { width: 30px; } Button { width: 200px; height: 50px; }");
        File.WriteAllText(
            Path.Combine(uiDirectory, "runtime-theme.css"),
            "Text { width: 40px; }");
        File.WriteAllText(
            Path.Combine(uiDirectory, "plain.xml"),
            "<Ui><Text id=\"plain\" /></Ui>");
    }

    private static void BakeLooseUi(string assets, string content)
    {
        foreach (var sourcePath in Directory.EnumerateFiles(
                     assets,
                     "*",
                     SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(assets, sourcePath);
            var extension = Path.GetExtension(sourcePath);
            var baker = extension.Equals(".xml", StringComparison.OrdinalIgnoreCase)
                ? (DreambitEngine.AssetBaker.Abstractions.IAssetBaker)new XmlbBaker()
                : extension.Equals(".css", StringComparison.OrdinalIgnoreCase)
                    ? new CssbBaker()
                    : null;
            if (baker is null)
                continue;

            var outputPath = Path.Combine(
                content,
                Path.ChangeExtension(relativePath, baker.OutputExtension));
            baker.Bake(new DreambitEngine.AssetBaker.Abstractions.BakeContext
            {
                InputPath = sourcePath,
                OutputPath = outputPath,
                LogicalRoot = assets
            });
        }
    }
}
