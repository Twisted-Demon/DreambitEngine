using Microsoft.Xna.Framework;

namespace Dreambit;

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

    public static ulong FrameCount { get; private set; }

    public static int FrameRate { get; private set; }

    public static float PhysicsDeltaTime { get; }
    public static float UnscaledPhysicsDeltaTime { get; }
    
    public static GameTime GameTime { get; private set; }

    internal static void Update(GameTime gameTime)
    {
        GameTime = gameTime;
        
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

    internal static void UpdatePhysicsTime()
    {
    }

    internal static void SceneLoaded()
    {
        TimeSinceSceneLoaded = 0.0f;
    }
}