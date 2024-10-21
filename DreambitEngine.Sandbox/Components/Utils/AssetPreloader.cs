using System.Collections.Generic;
using Dreambit.ECS;
using Dreambit.Scripting;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.Sandbox.Utils;

public class AssetPreloader : Component
{
    private readonly Logger<AssetPreloader> _logger = new();
    
    private Queue<string> _textureQueue = [];
    private Queue<string> _audioQueue = [];
    private Queue<string> _scriptQueue = [];

    private bool texturesLoaded = false;
    private bool audioLoaded = false;
    private bool scriptsLoaded = false;

    private float _timer;

    public void RegisterTextures(string[] paths)
    {
        foreach (var path in paths)
        {
            var pathWithoutExtension = path.Replace(".png", "");
            _textureQueue.Enqueue(pathWithoutExtension);
        }
    }

    public void RegisterAudios(string[] paths)
    {
        foreach (var path in paths)
        {
            var pathWithoutExtension = path.Replace(".mp3", "");
            _audioQueue.Enqueue(pathWithoutExtension);
        }
    }

    public void RegisterScripts(string[] paths)
    {
        foreach (var path in paths)
        {
            _scriptQueue.Enqueue(path);
        }
    }

    public override void OnUpdate()
    {
        if (!texturesLoaded)
        {
            _logger.Debug("Loading Textures");
            var path = _textureQueue.Dequeue();
            Resources.LoadAsset<Texture2D>(path);

            if (_textureQueue.Count == 0)
            {
                texturesLoaded = true;
                _logger.Debug("Textures Loaded");
            }
            return;
        }

        if (!audioLoaded)
        {
            _logger.Debug("Loading Audio");
            var path = _audioQueue.Dequeue();
            Resources.LoadAsset<SoundEffect>(path);
            
            if(_audioQueue.Count == 0)
                audioLoaded = true;

            return;
        }

        if (!scriptsLoaded)
        {
            _logger.Debug("Loading Scripts");
            var path = _scriptQueue.Dequeue();
            
            ScriptingManager.LoadSequenceFromFile(path, "");
            
            if(_scriptQueue.Count == 0)
                scriptsLoaded = true;

            return;
        }
        
        _timer += Time.DeltaTime;
        if (_timer < 2.0f)
            return;

        var scene = Scene.SetNextLDtkScene(Worlds.AriaWorld.Dev_world);
        scene.AddRenderer<DefaultRenderer>();
    }
}