using System;
using Microsoft.Xna.Framework;

namespace PixelariaEngine;

public class StopWatch
{
    private TimeSpan _elapsedTime;
    private TimeSpan _startTime;
    private bool _isRunning;

    public StopWatch()
    {
        _elapsedTime = TimeSpan.Zero;
        _isRunning = false;
    }

    public void Start(GameTime gameTime)
    {
        
    }
}