using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Dreambit.ECS.Audio;

[BlueprintType("SoundEffectEmitter")]
public class SoundEffectEmitter : Component
{
    private float _masterVolume = 1.0f;
    private SoundEffectInstance[] _pool = new SoundEffectInstance[5];
    private int _poolIdx;

    private SoundEffectInstance _primaryInstance;
    private SoundCue _soundCue;

    private string _soundCuePath;
    public bool CullWhenOffscreen { get; set; } = false;

    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            var volume = Mathf.Clamp(value, 0.0f, 1.0f);
            _masterVolume = volume;
        }
    }

    public override void OnUpdate()
    {
        for (var i = 0; i < _pool.Length; i++)
        {
            var inst = _pool[i];
            if (inst != null && inst.State == SoundState.Stopped)
            {
                inst.Dispose();
                _pool[i] = null;
            }
        }
    }

    public override void OnDestroyed()
    {
        StopAll();
    }

    private void UpdateSoundCue(SoundCue soundCue)
    {
        _soundCuePath = soundCue.AssetName;
        _soundCue = soundCue;

        for (var i = 0; i < _pool.Length; i++)
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
        for (var i = 0; i < _pool.Length; i++)
        {
            _pool[i]?.Stop();
            _pool[i]?.Dispose();
            _pool[i] = null;
        }
    }

    public void PauseAll()
    {
        _primaryInstance?.Pause();
        for (var i = 0; i < _pool.Length; i++) _pool[i]?.Pause();
    }

    public void ResumeAll()
    {
        _primaryInstance?.Resume();
        for (var i = 0; i < _pool.Length; i++) _pool[i]?.Resume();
    }

    public void Play(SoundCue cue)
    {
        if (cue.Takes.Length == 0) return;

        var activeCount = CountActive();
        if (activeCount >= cue.MaxOverlaps && !cue.Loop) return;

        var sfxInstance = cue.GetSfxInstance();

        var currentVolume = cue.Volume * MasterVolume;
        var currentJitter = cue.VolumeJitter * MasterVolume;
        sfxInstance.Volume = GetValueWithJitter(currentVolume, currentJitter);

        sfxInstance.Pitch = GetValueWithJitter(cue.Pitch, cue.PitchJitter);
        sfxInstance.Pan = cue.Pan;

        sfxInstance.IsLooped = cue.Loop;

        if (cue.Loop)
        {
            if (_primaryInstance == null || _primaryInstance.IsDisposed)
            {
                _primaryInstance = sfxInstance;
                _primaryInstance?.Play();
            }

            var inst = _primaryInstance;
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

        for (var i = 0; i < _pool.Length; i++)
            if (_pool[i] != null && _pool[i].State == SoundState.Playing)
                c++;

        return c;
    }

    private float GetValueWithJitter(float baseValue, Vector2 jitter)
    {
        var min = baseValue - jitter.X;
        var max = baseValue + jitter.Y;

        var value = Random.Shared.NextFloat(min, max);

        return value;
    }

    private float GetValueWithJitter(float baseValue, Vector2 jitter, float min, float max)
    {
        return Mathf.Clamp(GetValueWithJitter(baseValue, jitter), min, max);
    }

    private SoundEffectInstance NextPoolSlot()
    {
        _poolIdx = (_poolIdx + 1) % _pool.Length;
        return _pool[_poolIdx];
    }
}