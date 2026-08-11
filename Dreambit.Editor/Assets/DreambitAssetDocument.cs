using System.Collections;
using Dreambit.Editor.Inspection;
using Dreambit.Editor.Undo;
using Newtonsoft.Json.Linq;

namespace Dreambit.Editor.Assets;

internal sealed class DreambitAssetDocument : IDisposable
{
    private readonly InspectorMetadataCache _metadata;
    private JObject _source;
    private bool _disposed;

    private DreambitAssetDocument(
        AssetRecord asset,
        Type assetType,
        DreambitAsset instance,
        JObject source,
        InspectorMetadataCache metadata)
    {
        Asset = asset;
        AssetType = assetType;
        Instance = instance;
        _source = source;
        _metadata = metadata;
        Undo = new UndoService();
    }

    public AssetRecord Asset { get; }
    public Type AssetType { get; }
    public DreambitAsset Instance { get; private set; }
    public UndoService Undo { get; }
    public bool IsDirty { get; private set; }
    public DateTimeOffset LastChangedUtc { get; private set; }
    public event Action<DreambitAssetDocument>? Changed;

    public static DreambitAssetDocument Open(
        AssetRecord asset,
        string path,
        Type assetType,
        InspectorMetadataCache metadata)
    {
        var json = File.ReadAllText(path);
        var source = JObject.Parse(json);
        var instance = DreambitJson.Deserialize(json, assetType) as DreambitAsset
                       ?? throw new InvalidDataException($"'{asset.RelativePath}' is not a {assetType.FullName} asset.");
        instance.AssetId = asset.Id;
        instance.AssetName = asset.LogicalAssetName;
        return new DreambitAssetDocument(asset, assetType, instance, source, metadata);
    }

    public void Apply(string name, Action<DreambitAsset> mutation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var before = CaptureJson();
        mutation(Instance);
        var after = CaptureJson();
        if (string.Equals(before, after, StringComparison.Ordinal))
            return;
        IsDirty = true;
        LastChangedUtc = DateTimeOffset.UtcNow;
        Undo.Record(new AssetSnapshotCommand(name, this, before, after));
        Changed?.Invoke(this);
    }

    public void Save(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var json = CaptureJson();
        var temporary = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporary, json);
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
        IsDirty = false;
    }

    public string CaptureJson()
    {
        var merged = (JObject)_source.DeepClone();
        foreach (var member in _metadata.Get(AssetType, InspectorTargetKind.Asset))
        {
            var value = member.GetValue(Instance);
            merged[member.SerializedName] = SerializeValue(value, member.ValueType);
        }
        _source = merged;
        return merged.ToString(Newtonsoft.Json.Formatting.Indented);
    }

    private static JToken SerializeValue(object? value, Type declaredType)
    {
        if (value is null)
            return JValue.CreateNull();
        if (value is DreambitAsset asset)
        {
            if (!asset.AssetId.IsEmpty)
                return DreambitAssetReferenceToken.Create(asset.AssetId, asset.AssetName);
            if (!string.IsNullOrWhiteSpace(asset.AssetName))
                return new JValue(asset.AssetName);
        }
        if (value is IDictionary dictionary)
        {
            var valueType = declaredType.IsGenericType
                ? declaredType.GetGenericArguments()[1]
                : typeof(object);
            var result = new JObject();
            foreach (DictionaryEntry entry in dictionary)
                result[Convert.ToString(entry.Key) ?? string.Empty] = SerializeValue(entry.Value, valueType);
            return result;
        }
        if (value is IEnumerable sequence && value is not string)
        {
            var elementType = declaredType.IsArray
                ? declaredType.GetElementType() ?? typeof(object)
                : declaredType.IsGenericType
                    ? declaredType.GetGenericArguments()[0]
                    : typeof(object);
            var result = new JArray();
            foreach (var item in sequence)
                result.Add(SerializeValue(item, elementType));
            return result;
        }
        return DreambitJson.ToToken(value);
    }

    public void CopyInspectableValuesTo(DreambitAsset target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.GetType() != AssetType)
            throw new ArgumentException(
                $"Preview target '{target.GetType().FullName}' is not '{AssetType.FullName}'.",
                nameof(target));

        foreach (var member in _metadata.Get(AssetType, InspectorTargetKind.Asset))
        {
            if (member.IsReadOnly)
                continue;
            var value = member.GetValue(Instance);
            object? copy;
            if (value is DreambitAsset)
            {
                copy = value;
            }
            else
            {
                var token = SerializeValue(value, member.ValueType);
                copy = token.Type == JTokenType.Null
                    ? null
                    : DreambitJson.FromToken(token, member.ValueType);
            }
            member.SetValue(target, copy);
        }
    }

    private void Restore(string json, bool dirty)
    {
        _source = JObject.Parse(json);
        var replacement = DreambitJson.Deserialize(json, AssetType) as DreambitAsset
                          ?? throw new InvalidDataException("Could not restore the asset snapshot.");
        replacement.AssetId = Asset.Id;
        replacement.AssetName = Asset.LogicalAssetName;
        Instance.Dispose();
        Instance = replacement;
        IsDirty = dirty;
        LastChangedUtc = DateTimeOffset.UtcNow;
        Changed?.Invoke(this);
    }

    internal void RestoreReloadSnapshot(string json, bool dirty) => Restore(json, dirty);

    public void Dispose()
    {
        if (_disposed)
            return;
        Instance.Dispose();
        Undo.Clear();
        _disposed = true;
    }

    private sealed record AssetSnapshotCommand(
        string Name,
        DreambitAssetDocument Document,
        string Before,
        string After) : IUndoableEditorCommand
    {
        public void Undo() => Document.Restore(Before, false);
        public void Redo() => Document.Restore(After, true);
    }
}
