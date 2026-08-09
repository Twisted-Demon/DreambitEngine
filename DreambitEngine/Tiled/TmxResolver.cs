using System.IO;

namespace Dreambit.Tiled;

public static class TmxResolver
{
    public static void ResolveTmxMap(TmxMap map)
    {
        for(int i = 0; i < map.Tilesets.Count; i++)
        {
            var tileset = map.Tilesets[i];

            if (!string.IsNullOrEmpty(tileset.Source))
            {
                var mapPath = map.AssetName;
                var tileSetSource = map.Tilesets[i].Source;
                tileSetSource = Path.ChangeExtension(tileSetSource, null);
                var tileSetPath = ResolveRelativeAssetPath(mapPath, tileSetSource);

                map.Tilesets[i] = Resources.LoadAsset<TmxTileset>(tileSetPath);
            }
        }
    }

    public static string ResolveRelativeAssetPath(string assetPath, string relativePath)
    {
        var assetDirectory = Path.GetDirectoryName(assetPath) ?? string.Empty;

        var fakeRoot = Path.GetFullPath(".");

        var absolutePath = Path.GetFullPath(
            Path.Combine(fakeRoot, assetDirectory, relativePath));

        return Path.GetRelativePath(fakeRoot, absolutePath);
    }
}
