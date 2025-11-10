using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FontStashSharp;
using LDtk;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace Dreambit;

public class Resources : Singleton<Resources>
{
    private List<IDisposable> _disposableAssets;

    private Dictionary<string, object> _loadedAssets;
    private ContentManager Content { get; } = Core.Instance.Content;
    private static string ContentDirectory => Path.Combine(AppContext.BaseDirectory, Instance.Content.RootDirectory);
    public static bool UsePak { get; set; } = true;
    public static string PakName { get; set; } = "content.pak";

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

    private static readonly Dictionary<Type, IAssetLoader> Loaders = new()
    {
        { typeof(Texture2D), new Texture2dLoader() },
        { typeof(SoundEffect), new SoundEffectLoader() },
        { typeof(Song), new SongLoader() },
        { typeof(SpriteSheet), new SpriteSheetLoader() },
        { typeof(SpriteSheetAnimation), new SpriteSheetAnimationLoader() },
        { typeof(LDtkFile), new LDtkFileLoader() },
        { typeof(LDtkLevel), new LDtkLevelLoader() },
        { typeof(SoundCue), new SoundCueLoader() },
        { typeof(SpriteFontBaseLoader), new SpriteFontBaseLoader()},
        { typeof(EntityBlueprint), new EntityBlueprintLoader()}
    };

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
            if (rawAsset is T asset)
                return asset;
        
        try
        {
            Instance.Logger.Trace("Loading {0} - {1}", typeof(T).Name, assetName);
            
            object asset;
            if (Loaders.TryGetValue(typeof(T), out var loader))
            {
                asset = loader.Load(assetName, PakName, UsePak, ContentDirectory);
            }
            else
            {
                asset = Instance.Content.Load<T>(assetName);
            }

            Instance.LoadedAssets[assetName] = (T)asset;
            
            if (asset is IDisposable disposable)
                Instance.DisposableAssets.Add(disposable);

            return (T)asset;
        }
        catch (Exception e)
        {
            Instance.Logger.Warn("Could not load {0} | {1}", typeof(T).Name, assetName);
            Instance.Logger.Error(e.Message);

            return null;
        }
    }
    
    public static object LoadDreambitAsset(string assetName, Type type)
    {
        if (!type.IsSubclassOf(typeof(DreambitAsset)))
            return null;
        
        //if we already have the asset we will return it
        if (Instance.LoadedAssets.TryGetValue(assetName, out var rawAsset))
            return rawAsset;
        
        try
        {
            Instance.Logger.Trace("Loading {0} - {1}", type.Name, assetName);

            object asset = null;
            if (Loaders.TryGetValue(type, out var loader))
            {
                asset = loader.Load(assetName, PakName, UsePak, ContentDirectory);
            }

            Instance.LoadedAssets[assetName] = asset;
            
            if (asset is IDisposable disposable)
                Instance.DisposableAssets.Add(disposable);

            return asset;
        }
        catch (Exception e)
        {
            Instance.Logger.Warn("Could not load {0} | {1}", type.Name, assetName);
            Instance.Logger.Error(e.Message);

            return null;
        }
    }

    public static void UnloadAsset(string assetName)
    {
        Instance.Content.UnloadAsset(assetName);
    }

    public static SpriteFontBase LoadSpriteFont(string assetName, float fontSize = 12f)
    {
        //if we already have the asset we will return it
        if (Instance.LoadedAssets.TryGetValue(assetName + fontSize, out var rawAsset))
            if (rawAsset is SpriteFontBase font)
                return font;

        try
        {
            Instance.Logger.Trace("Loading SpriteFontBase - {0}", assetName + fontSize);

            SpriteFontBase font;
            if (Loaders.TryGetValue(typeof(SpriteFontBaseLoader), out var loader))
            {
                var sfLoader = (SpriteFontBaseLoader)loader;
                
                font = sfLoader.LoadFont(assetName, ContentDirectory, fontSize);
            }
            else
            {
                Instance.Logger.Warn("Could not load {0} | {1}", nameof(SpriteFontBase), assetName + fontSize);
                return null;
            }
            
            Instance.LoadedAssets[assetName + fontSize] = font;

            var disposable = font as IDisposable;
            if (disposable != null)
                Instance.DisposableAssets.Add(disposable);

            return font;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    
    private static Texture2D PremultiplyTexture(Texture2D texture)
    {
        var data = new Color[texture.Width * texture.Height];
        texture.GetData(data);

        for (var i = 0; i < data.Length; i++)
        {
            var c = data[i];
            var alpha = c.A / 255f;
            data[i] = new Color((byte)(c.R * alpha), (byte)(c.G * alpha), (byte)(c.B * alpha), c.A);
        }

        var result = new Texture2D(texture.GraphicsDevice, texture.Width, texture.Height);
        result.SetData(data);
        return result;
    }
}