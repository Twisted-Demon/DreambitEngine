using System;

namespace Dreambit.UI;

internal static class UiAssetPath
{
    public static string ToBakedXml(string sourcePath)
    {
        if (sourcePath.EndsWith(".xmlb", StringComparison.OrdinalIgnoreCase))
            return sourcePath;
        if (sourcePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            return sourcePath[..^".xml".Length] + ".xmlb";
        return sourcePath + ".xmlb";
    }

    public static string ToBakedStylesheet(string sourcePath)
    {
        if (sourcePath.EndsWith(".cssb", StringComparison.OrdinalIgnoreCase))
            return sourcePath;
        if (sourcePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
            return sourcePath[..^".css".Length] + ".cssb";
        return sourcePath + ".cssb";
    }

    public static string GetSiblingStylesheet(string documentPath)
    {
        if (documentPath.EndsWith(".xmlb", StringComparison.OrdinalIgnoreCase))
            return documentPath[..^".xmlb".Length] + ".css";
        if (documentPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            return documentPath[..^".xml".Length] + ".css";
        return documentPath + ".css";
    }
}
