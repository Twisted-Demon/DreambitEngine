using DreambitEngine.AssetBaker.Abstractions;

namespace DreambitEngine.AssetBaker.Pipeline;

public sealed record AssetBlob(
    string LogicalPath,
    AssetType Type,
    string Extension,
    byte[] Data
);