using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine;

public class Resources
{
    public static ContentManager Content => Core.Instance.Content;
    private static readonly Logger<Resources> _logger = new();
    
    /// <summary>
    /// Tries to Load an asset and returns null if not found 
    /// </summary>
    /// <param name="path"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T Load<T>(string path) where T : class
    {
        _logger.Trace("loading asset - {0}", path);
        try
        {
            return Core.Instance.Content.Load<T>(path);
        }
        catch (Exception e)
        {
            _logger.Warn("Could not load resource {0}", path);
            _logger.Debug("Exception: {0}", e);

            return null;
        }
    }
}