using System;
using Microsoft.Xna.Framework.Content;

namespace Dreambit;

/// <summary>
///     Uses MonoGame's XNB readers without entering assets into ContentManager's
///     internal loaded/disposable collections. Dreambit owns both concerns.
/// </summary>
internal sealed class DreambitXnbReader : ContentManager
{
    public DreambitXnbReader(IServiceProvider serviceProvider, string rootDirectory)
        : base(serviceProvider, rootDirectory)
    {
    }

    public T Read<T>(string assetName, Action<IDisposable> recordDisposable)
    {
        return ReadAsset<T>(assetName, recordDisposable);
    }
}
