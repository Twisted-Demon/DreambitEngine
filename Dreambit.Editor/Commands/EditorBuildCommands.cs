using Dreambit.Editor.Assets;
using Dreambit.Editor.Compilation;

namespace Dreambit.Editor.Commands;

/// <summary>
/// The shared semantic operations for editor build and bake affordances.
/// </summary>
internal sealed class EditorBuildCommands(
    GameCodeService gameCode,
    AssetBakeService assetBaking)
{
    public GameBuildStatus GameBuildStatus => gameCode.Status;
    public bool IsGameBuildRunning => gameCode.IsRunning;
    public GameAssemblyLoadService Assemblies => gameCode.Assemblies;

    public void BuildGame() => gameCode.RequestBuild(rebuild: false, immediate: true);
    public void RebuildGame() => gameCode.RequestBuild(rebuild: true, immediate: true);
    public void UpdateBlobs() => assetBaking.RequestBake(rebuildAll: false);
    public void RebuildAllBlobs() => assetBaking.RequestBake(rebuildAll: true);
    public void BakePak() => assetBaking.RequestPakBake();
}
