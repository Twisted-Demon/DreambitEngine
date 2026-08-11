using Dreambit.Editor.Assets;
using Dreambit.Editor.Compilation;
using Dreambit.Editor.Scenes;
using Dreambit.Editor.Inspection;

namespace Dreambit.Editor.Projects;

internal sealed class DreambitProjectSession : IDisposable
{
    private readonly ProjectInstanceLease _lease;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _disposed;

    public DreambitProjectSession(
        DreambitProjectDefinition project,
        ProjectInstanceLease lease,
        Action<AssetDatabaseDiagnostic>? reportAssetDiagnostic = null,
        Action<AssetBakeMessage>? reportAssetBake = null,
        Action<GameCodeMessage>? reportGameCode = null,
        Action<string, Exception?>? reportSceneError = null)
    {
        Project = project;
        _lease = lease;
        Assets = new AssetDatabase(
            project.RootDirectory,
            project.ContentRootPath,
            reportAssetDiagnostic);
        AssetBaking = new AssetBakeService(
            project,
            Assets,
            _lifetime.Token,
            reportAssetBake);
        Resources.SetContentSource(Path.GetDirectoryName(AssetBaking.OutputPakPath)!);
        GameCode = new GameCodeService(
            project,
            _lifetime.Token,
            reportGameCode);
        InspectorMetadata = new InspectorMetadataCache();
        EditorTypes = new EditorTypeRegistry(GameCode.Assemblies, InspectorMetadata);
        CustomEditors = new CustomEditorRegistry(GameCode.Assemblies, reportSceneError);
        Scenes = new SceneDocumentService(
            project,
            GameCode.Assemblies,
            reportSceneError);
        AssetEditing = new AssetEditingService(
            project,
            Assets,
            EditorTypes,
            InspectorMetadata,
            GameCode.Assemblies,
            reportSceneError);
        Scenes.Selection.Changed += OnEntitySelectionChanged;
        AssetBaking.BakeCompleted += OnBakeCompleted;
        Resources.AssetRegistry = Assets;
    }

    public DreambitProjectDefinition Project { get; }
    public AssetDatabase Assets { get; }
    public AssetBakeService AssetBaking { get; }
    public GameCodeService GameCode { get; }
    public SceneDocumentService Scenes { get; }
    public InspectorMetadataCache InspectorMetadata { get; }
    public EditorTypeRegistry EditorTypes { get; }
    public AssetEditingService AssetEditing { get; }
    public CustomEditorRegistry CustomEditors { get; }
    public CancellationToken LifetimeToken => _lifetime.Token;
    public bool IsDisposed => _disposed;

    public void Dispose()
    {
        if (_disposed)
            return;

        _lifetime.Cancel();
        AssetBaking.BakeCompleted -= OnBakeCompleted;
        Scenes.Selection.Changed -= OnEntitySelectionChanged;
        AssetEditing.Dispose();
        Scenes.Dispose();
        CustomEditors.Dispose();
        EditorTypes.Dispose();
        GameCode.Dispose();
        AssetBaking.Dispose();
        if (ReferenceEquals(Resources.AssetRegistry, Assets))
            Resources.AssetRegistry = null;
        Assets.Dispose();
        _lifetime.Dispose();
        _lease.Dispose();
        _disposed = true;
    }

    private void OnBakeCompleted(DreambitEngine.AssetBaker.Pipeline.AssetBakeResult _)
    {
        Scenes.ReloadContent();
    }

    private void OnEntitySelectionChanged()
    {
        if (Scenes.Selection.EntityIds.Count > 0)
            AssetEditing.Clear();
    }
}
