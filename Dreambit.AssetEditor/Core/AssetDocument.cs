using Newtonsoft.Json.Linq;

namespace Dreambit.AssetEditor.Core;

internal sealed class AssetDocument(Type assetType, JObject json)
{
    public Type AssetType { get; } = assetType;
    public JObject Json { get; } = json;
    public string? FilePath { get; set; }
    public bool IsDirty { get; set; }

    public string DisplayName => FilePath is null
        ? $"Untitled {AssetType.Name}"
        : Path.GetFileName(FilePath);
}
