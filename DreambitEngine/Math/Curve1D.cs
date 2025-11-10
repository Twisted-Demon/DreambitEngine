using System;
using System.Runtime.CompilerServices;

namespace Dreambit;

public class Curve1D
{
    public readonly struct Key
    {
        public readonly float Time;
        public readonly float Value;
        public Key(float time, float value)
        {
            Time = time;
            Value = value;
        }
    }

    private readonly Key[] _keys;
    public ReadOnlySpan<Key> Keys => _keys;

    public Curve1D(params Key[] keys)
    {
        if (keys == null || keys.Length == 0)
            keys = new[]
            {
                new Key(0, 1),
                new Key(1, 0),
            };
        Array.Sort(keys, (a, b) => a.Time.CompareTo(b.Time));
        _keys = keys;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Evaluate(float time)
    {
        if (time <= _keys[0].Time) return _keys[0].Value;
        var last = _keys[^1];
        if (time >= last.Time) return last.Value;

        // binary search for segment
        int lo = 0, hi = _keys.Length - 1;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) >> 1;
            if (time < _keys[mid].Time) hi = mid; else lo = mid;
        }

        var a = _keys[lo];
        var b = _keys[hi];
        float u = (time - a.Time) / (b.Time - a.Time);
        return a.Value + (b.Value - a.Value) * u;
    }
    
    // Handy presets (all clamped 0..1 on t)
    public static Curve1D FadeOut()     => new(new Key(0f, 1f), new Key(1f, 0f));
    public static Curve1D FadeIn()      => new(new Key(0f, 0f), new Key(1f, 1f));
    public static Curve1D EaseInOut()   => new(new Key(0f, 0f), new Key(0.5f, 1f), new Key(1f, 0f));
    public static Curve1D Bell()        => new(new Key(0f, 0f), new Key(0.5f, 1f), new Key(1f, 0f));
    public static Curve1D Flat(float v) => new(new Key(0f, v), new Key(1f, v));
}