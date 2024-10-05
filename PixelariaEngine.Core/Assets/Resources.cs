using System;
using Microsoft.Xna.Framework.Content;

namespace PixelariaEngine;

public class Resources
{
    private static readonly Logger<Resources> _logger = new();
    public static ContentManager Content => Core.Instance.Content;

    /// <summary>
    ///     Tries to Load an asset and returns null if not found
    /// </summary>
    /// <param name="path"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T Load<T>(string path) where T : class
    {
        try
        {
            var asset = Core.Instance.Content.Load<T>(path);
            _logger.Trace("Loaded {0} | {1}", typeof(T).Name, path);

            return asset;
        }
        catch (Exception e)
        {
            _logger.Warn("Could not load {0} | {1}", typeof(T).Name, path);
            return null;
        }
    }
}