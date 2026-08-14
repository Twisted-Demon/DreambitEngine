using Dreambit.Editor.Assets;
using Dreambit.Editor.Compilation;
using Dreambit.Editor.Scenes;
using Dreambit.Editor.Inspection;

namespace Dreambit.Editor.Projects;

internal sealed class DreambitProjectSession : IDisposable
{
    private readonly ProjectInstanceLease _lease;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Action<string, Exception?>? _reportError;
    private readonly IAssetRegistry? _previousAssetRegistry;
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
        _reportError = reportSceneError;
        _previousAssetRegistry = Resources.AssetRegistry;
        try
        {
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
            AssetEditing = new AssetEditingService(
                project,
                Assets,
                EditorTypes,
                InspectorMetadata,
                GameCode.Assemblies,
                reportSceneError);
            BlueprintSources = new BlueprintSourceService(Assets, AssetEditing);
            Scenes = new SceneDocumentService(
                project,
                GameCode.Assemblies,
                Assets,
                BlueprintSources,
                reportSceneError);
            Blueprints = new BlueprintEditingService(
                AssetEditing,
                GameCode.Assemblies,
                BlueprintSources,
                reportSceneError);
            Documents = new EditorDocumentContext(Scenes, Blueprints, AssetEditing);
            AssetBaking.BakeCompleted += OnBakeCompleted;
            Resources.AssetRegistry = Assets;
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public DreambitProjectDefinition Project { get; }
    public AssetDatabase Assets { get; private set; } = null!;
    public AssetBakeService AssetBaking { get; private set; } = null!;
    public GameCodeService GameCode { get; private set; } = null!;
    public SceneDocumentService Scenes { get; private set; } = null!;
    public BlueprintEditingService Blueprints { get; private set; } = null!;
    public BlueprintSourceService BlueprintSources { get; private set; } = null!;
    public EditorDocumentContext Documents { get; private set; } = null!;
    public InspectorMetadataCache InspectorMetadata { get; private set; } = null!;
    public EditorTypeRegistry EditorTypes { get; private set; } = null!;
    public AssetEditingService AssetEditing { get; private set; } = null!;
    public CustomEditorRegistry CustomEditors { get; private set; } = null!;
    public CancellationToken LifetimeToken => _lifetime.Token;
    public bool IsDisposed => _disposed;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        TryDispose(_lifetime.Cancel, "Could not cancel the project lifetime.");
        if (AssetBaking is not null)
            AssetBaking.BakeCompleted -= OnBakeCompleted;
        TryDispose(() => Blueprints?.Dispose(), "Could not dispose Blueprint editing.");
        TryDispose(() => Scenes?.Dispose(), "Could not dispose scene documents.");
        TryDispose(() => BlueprintSources?.Dispose(), "Could not dispose Blueprint sources.");
        TryDispose(() => AssetEditing?.Dispose(), "Could not dispose asset editing.");
        TryDispose(() => CustomEditors?.Dispose(), "Could not dispose custom editors.");
        TryDispose(() => EditorTypes?.Dispose(), "Could not dispose editor type metadata.");
        TryDispose(() => GameCode?.Dispose(), "Could not dispose game compilation.");
        TryDispose(() => AssetBaking?.Dispose(), "Could not dispose asset baking.");
        if (Assets is not null && ReferenceEquals(Resources.AssetRegistry, Assets))
            Resources.AssetRegistry = _previousAssetRegistry;
        TryDispose(() => Assets?.Dispose(), "Could not dispose the asset database.");
        TryDispose(_lifetime.Dispose, "Could not dispose the project lifetime.");
        TryDispose(_lease.Dispose, "Could not release the project lock.");
    }

    private void OnBakeCompleted(DreambitEngine.AssetBaker.Pipeline.AssetBakeResult _)
    {
        AssetEditing.BeforeContentReload();
        try
        {
            Scenes.ClearBlueprintPreviews();
            Scenes.ReloadContent();
        }
        finally
        {
            AssetEditing.AfterContentReload();
        }
    }

    private void TryDispose(Action action, string message)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            try
            {
                _reportError?.Invoke(message, exception);
            }
            catch
            {
                Console.Error.WriteLine($"{message} {exception}");
            }
        }
    }

}
