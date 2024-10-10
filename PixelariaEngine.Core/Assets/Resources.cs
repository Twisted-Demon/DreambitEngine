using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;

namespace PixelariaEngine;

public class Resources : Singleton<Resources>
{
    private ContentManager Content { get; } = Core.Instance.Content;
    private List<IDisposable> _disposableAssets;
    internal List<IDisposable> DisposableAssets
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
    internal Dictionary<string, object> LoadedAssets
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
            var asset = Instance.Content.Load<T>(assetName);
            Instance.Logger.Debug("Loaded {0} | {1}", typeof(T).Name, assetName);

            return asset;
        }
        catch (Exception e)
        {
            Instance.Logger.Warn("Could not load {0} | {1}", typeof(T).Name, assetName);
            Instance.Logger.Error(e.Message);

            return default;
        }
    }
}