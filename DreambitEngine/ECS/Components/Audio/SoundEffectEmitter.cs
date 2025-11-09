using Microsoft.Xna.Framework.Audio;

namespace Dreambit.ECS.Audio;

public class SoundEffectEmitter : Component<SoundEffectEmitter>
{
    private SoundCue _soundCue;

    public SoundCue SoundCue
    {
        get => _soundCue;
        set
        {
            if(value is null) return;
            UpdateSoundCue(value);
        }
    }
    
    private string _soundCuePath;

    public string SoundCuePath
    {
        get => _soundCuePath;
        set
        {
            var cue = Resources.LoadAsset<SoundCue>(value);

            if (cue is null) return;
            UpdateSoundCue(cue);
        }
    }

    private SoundEffectInstance _primaryInstance;
    private SoundEffectInstance[] _pool = new SoundEffectInstance[5];
    private int _poolIdx = 0;

    public bool CullWhenOffscreen { get; set; } = false;

    public override void OnUpdate()
    {
        for (int i = 0; i < _pool.Length; i++)
        {
            var inst = _pool[i];
            if (inst != null && inst.State == SoundState.Stopped)
            {
                inst.Dispose();
                _pool[i] = null;
            }
        }
    }

    private void UpdateSoundCue(SoundCue soundCue)
    {
        _soundCuePath = soundCue.AssetName;
        _soundCue = soundCue;

        for (int i = 0; i < _pool.Length; i++)
        {
            var inst = _pool[i];
            if (inst != null)
            {
                inst.Dispose();
                _pool[i] = null;
            }
        }

        _pool = new SoundEffectInstance[soundCue.Takes.Length];
    }

    public void StopAll()
    {
        _primaryInstance?.Stop();
        _primaryInstance?.Dispose();
        _primaryInstance = null;
        for (int i = 0; i < _pool.Length; i++)
        {
            _pool[i]?.Stop();
            _pool[i]?.Dispose();
            _pool[i] = null;
        }
    }

    public void PauseAll()
    {
        _primaryInstance?.Pause();
        for (int i = 0; i < _pool.Length; i++)
        {
            _pool[i]?.Pause();
        }
    }
    
    public void ResumeAll()
    {
        _primaryInstance?.Resume();
        for (int i = 0; i < _pool.Length; i++)
        {
            _pool[i]?.Resume();
        }
    }

    public void Play(SoundCue cue)
    {
        if (cue.Takes.Length == 0) return;

        int activeCount = CountActive();
        if (activeCount >= cue.MaxOverlaps && !cue.Loop) return;

        var sfxInstance = cue.GetSfxInstance();
        
        sfxInstance.Volume = cue.Volume;
        sfxInstance.Pitch = cue.Pitch;
        sfxInstance.Pan = cue.Pan;

        sfxInstance.IsLooped = cue.Loop;

        if (cue.Loop)
        {
            if(_primaryInstance == null || _primaryInstance.IsDisposed)
                _primaryInstance = sfxInstance;
            
            var inst =  _primaryInstance;
            
        }
        else
        {
            var slot = NextPoolSlot();
            slot?.Stop();
            slot?.Dispose();

            sfxInstance.Play();
            _pool[_poolIdx] = sfxInstance;
        }
    }

    private int CountActive()
    {
        var c = 0;
        
        if (_primaryInstance != null && _primaryInstance.State == SoundState.Playing) c++;
        
        for(int i = 0; i < _pool.Length; i++)
            if(_pool[i] != null && _pool[i].State == SoundState.Playing) c++;

        return c;
    }

    private SoundEffectInstance NextPoolSlot()
    {
        _poolIdx = (_poolIdx + 1) % _pool.Length;
        return _pool[_poolIdx];
    }
}