using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace Dreambit;

public class AudioSystem : Singleton<AudioSystem>
{
    private readonly List<AudioEmitter> _emitters = [];

    private readonly List<AudioListener> _listeners = [];

    public IReadOnlyList<AudioListener> Listeners => _listeners;
    public IReadOnlyList<AudioEmitter> Emitters => _emitters;

    public void RegisterEmitter(AudioEmitter emitter)
    {
        if (!_emitters.Contains(emitter))
            _emitters.Add(emitter);
    }

    public void DeregisterEmitter(AudioEmitter emitter)
    {
        _emitters.Remove(emitter);
    }

    public void RegisterListener(AudioListener listener)
    {
        if (!_listeners.Contains(listener))
            _listeners.Add(listener);
    }

    public void DeregisterListener(AudioListener listener)
    {
        _listeners.Remove(listener);
    }

    public void CleanUp()
    {
        _emitters.Clear();
        _listeners.Clear();
    }
}