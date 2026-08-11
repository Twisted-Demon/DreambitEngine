using System;
using Microsoft.Xna.Framework;

namespace Dreambit;

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
        ArgumentNullException.ThrowIfNull(gameTime);
        if (_isRunning) return;

        _startTime = gameTime.TotalGameTime;
        _isRunning = true;
    }

    public void Start()
    {
        Start(GetCurrentGameTime());
    }

    public void Stop(GameTime gameTime)
    {
        ArgumentNullException.ThrowIfNull(gameTime);
        if (!_isRunning) return;

        _elapsedTime += gameTime.TotalGameTime - _startTime;
        _isRunning = false;
    }

    public void Stop()
    {
        Stop(GetCurrentGameTime());
    }

    public void Reset()
    {
        _elapsedTime = TimeSpan.Zero;
        _startTime = TimeSpan.Zero;
        _isRunning = false;
    }

    public void Restart(GameTime gameTime)
    {
        Reset();
        Start(gameTime);
    }

    public void Restart()
    {
        Restart(GetCurrentGameTime());
    }

    public bool IsRunning => _isRunning;

    public TimeSpan Elapsed => _isRunning
        ? GetElapsed(GetCurrentGameTime())
        : _elapsedTime;

    public TimeSpan GetElapsed(GameTime gameTime)
    {
        ArgumentNullException.ThrowIfNull(gameTime);

        return _isRunning
            ? _elapsedTime + gameTime.TotalGameTime - _startTime
            : _elapsedTime;
    }

    private static GameTime GetCurrentGameTime()
    {
        return Time.GameTime ??
               throw new InvalidOperationException(
                   "The game clock has not started. Pass a GameTime instance explicitly.");
    }
}
