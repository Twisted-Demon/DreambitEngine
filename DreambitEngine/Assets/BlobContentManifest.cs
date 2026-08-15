using System;
using System.Collections.Generic;

namespace Dreambit;

/// <summary>
/// Describes the baked blobs available to development builds. The manifest is intentionally
/// small: loaders only need to translate a logical asset path into a cache-relative blob path.
/// </summary>
public sealed class BlobContentManifest
{
    public const int CurrentSchemaVersion = 1;
    public const string FileName = "content.blobs.json";
    public const string FingerprintFileName = "content.blobs.fingerprint";

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public string Fingerprint { get; set; } = string.Empty;
    public List<BlobContentEntry> Assets { get; set; } = [];
}

public sealed class BlobContentEntry
{
    public string Path { get; set; } = string.Empty;
    public string Blob { get; set; } = string.Empty;
}
