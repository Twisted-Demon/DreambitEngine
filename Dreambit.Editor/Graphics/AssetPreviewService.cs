using Dreambit.Editor.Assets;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.Editor.Graphics;

internal sealed class AssetPreviewService : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly ImGuiRenderer _imGui;
    private readonly string _contentRoot;
    private Texture2D? _texture;
    private nint _textureId;
    private AssetId _assetId;
    private DateTimeOffset _lastWrite;

    public AssetPreviewService(GraphicsDevice device, ImGuiRenderer imGui, string contentRoot)
    {
        _device = device;
        _imGui = imGui;
        _contentRoot = contentRoot;
    }

    public bool TryGetTexture(AssetRecord asset, out nint textureId, out int width, out int height)
    {
        textureId = 0;
        width = height = 0;
        if (asset.Kind != AssetKind.Texture)
            return false;
        if (_texture is null || _assetId != asset.Id || _lastWrite != asset.LastWriteUtc)
        {
            Clear();
            var path = Path.Combine(_contentRoot, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            using var stream = File.OpenRead(path);
            _texture = Texture2D.FromStream(_device, stream);
            _texture.Name = $"Dreambit Editor preview: {asset.RelativePath}";
            _textureId = _imGui.BindTexture(_texture);
            _assetId = asset.Id;
            _lastWrite = asset.LastWriteUtc;
        }
        textureId = _textureId;
        width = _texture.Width;
        height = _texture.Height;
        return true;
    }

    private void Clear()
    {
        if (_textureId != 0)
            _imGui.UnbindTexture(_textureId);
        _texture?.Dispose();
        _texture = null;
        _textureId = 0;
        _assetId = AssetId.Empty;
    }

    public void Dispose() => Clear();
}
