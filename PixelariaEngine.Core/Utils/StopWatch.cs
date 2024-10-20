using System;
using Microsoft.Xna.Framework;

namespace PixelariaEngine;

public class StopWatch
{
    private TimeSpan _elapsedTime;
    private bool _isRunning;
    private TimeSpan _startTime;

    public StopWatch()
    {
        _elapsedTime = TimeSpan.Zero;
        _isRunning = false;
    }

    public void Start(GameTime gameTime)
    {
    }
}