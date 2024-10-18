using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine;

public class Resources : Singleton<Resources>
{
    private ContentManager Content { get; } = Core.Instance.Content;
    private List<IDisposable> _disposableAssets;
    private List<IDisposable> DisposableAssets
    {
        get
        {
            if (_disposableAssets != null) return _disposableAssets;
            
            var fieldInfo = ReflectionUtils.GetFieldInfo(typeof(ContentManager), "disposableAssets");
            _disposableAssets = fieldInfo.GetValue(Core.Instance.Content) as List<IDisposable>;
            return _disposableAssets;
        }
    }
    
    private Dictionary<string, object> _loadedAssets;
    private Dictionary<string, object> LoadedAssets
    {
        get
        {
            if (_loadedAssets != null) return _loadedAssets;
            var fieldInfo = ReflectionUtils.GetFieldInfo(typeof(ContentManager), "loadedAssets");
            _loadedAssets = fieldInfo.GetValue(Core.Instance.Content) as Dictionary<string, object>;
            return _loadedAssets;
        }
    }
    
    /// <summary>
    ///     Tries to Load an asset and returns default if not found
    /// </summary>
    /// <param name="assetName"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T LoadAsset<T>(string assetName) where T : class
    {
        //if we already have the asset we will return it
        if (Instance.LoadedAssets.TryGetValue(assetName, out var rawAsset))
        {
            if (rawAsset is T asset)
                return asset;
        }

        try
        {
            Instance.Logger.Trace("Loading {0} - {1}", typeof(T).Name, assetName);
            var asset = Instance.Content.Load<T>(assetName);
            Instance.Logger.Debug("Loaded {0} - {1}", typeof(T).Name, assetName);

            if (asset is Texture2D texture)
            {
                asset = PremultiplyTexture(texture) as T;
            }

            return asset;
        }
        catch (Exception e)
        {
            Instance.Logger.Warn("Could not load {0} | {1}", typeof(T).Name, assetName);
            Instance.Logger.Error(e.Message);

            return default;
        }
    }

    public static Task LoadAssetQueueAsync<T>(AssetQueue<T> assetQueue) where T : class
    {
        assetQueue.IsLoading = true;

        for (var i = 0; i < assetQueue.Count; i++)
        {
            if (!assetQueue.TryGetNext(out var assetName)) continue;
            
            var asset = LoadAsset<T>(assetName);

            assetQueue.AddAsset(assetName, asset);
        }

        return Task.CompletedTask;
    }
    
    private static Texture2D PremultiplyTexture(Texture2D texture)
    {
        Color[] data = new Color[texture.Width * texture.Height];
        texture.GetData(data);

        for (int i = 0; i < data.Length; i++)
        {
            Color c = data[i];
            float alpha = c.A / 255f;
            data[i] = new Color((byte)(c.R * alpha), (byte)(c.G * alpha), (byte)(c.B * alpha), c.A);
        }

        Texture2D result = new Texture2D(texture.GraphicsDevice, texture.Width, texture.Height);
        result.SetData(data);
        return result;
    }
}