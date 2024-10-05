using Microsoft.Xna.Framework;

namespace PixelariaEngine;

public static class Time
{
    public static float MaxDeltaTime = float.MaxValue;
    public static float TotalTime { get; private set; }

    public static float DeltaTime { get; private set; }

    public static float UnscaledDeltaTime { get; private set; }

    public static float AltDeltaTime { get; private set; }

    public static float TimeSinceSceneLoaded { get; private set; }

    public static float TimeScale { get; } = 1f;

    public static float AltTimeScale { get; } = 1f;

    public static uint FrameCount { get; private set; }
    
    public static int FrameRate { get; private set; }

    internal static void Update(GameTime gameTime)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (dt > MaxDeltaTime)
            dt = MaxDeltaTime;
        TotalTime += dt;
        DeltaTime = dt * TimeScale;
        AltDeltaTime = dt * AltTimeScale;
        UnscaledDeltaTime = dt;
        TimeSinceSceneLoaded += dt;
        FrameCount++;
        FrameRate = Mathf.RoundToInt(1 / DeltaTime);
    }

    internal static void SceneLoaded()
    {
        TimeSinceSceneLoaded = 0.0f;
    }
}