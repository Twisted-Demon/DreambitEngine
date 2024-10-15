using System;
using System.Collections.Generic;
using PixelariaEngine.Graphics;

namespace PixelariaEngine.ECS;

[Require(typeof(SpriteDrawer))]
public class SpriteAnimator : Component
{
    public bool IsPlaying { get; private set; }
    
    //internals
    private SpriteDrawer _spriteDrawer;
    private SpriteSheetAnimation _currentAnimation;
    private Queue<SpriteSheetAnimation> _animationQueue = [];
    private float _elapsedFrameTime;
    private float _timeToNextFrame;
    private int _currentAnimationFrame;

    private readonly Logger<SpriteAnimator> _logger = new();

    public SpriteSheetAnimation Animation
    {
        get => _currentAnimation;
        set
        {
            if (_currentAnimation == value) return;
            UpdateAnimation(value); 
        }
    }

    public override void OnCreated()
    {
        _spriteDrawer = Entity.GetComponent<SpriteDrawer>();
        _spriteDrawer.PivotType = PivotType.Custom;
    }

    public override void OnUpdate()
    {
        if (_currentAnimation == null)
            return;

        if (!IsPlaying)
            return;
        
        Run();
    }

    private void Run()
    {
        _elapsedFrameTime += Time.DeltaTime;

        if (!(_elapsedFrameTime >= _timeToNextFrame)) return;
        
        _elapsedFrameTime -= _timeToNextFrame;
        ChangeAnimationFrame();
    }

    private void ChangeAnimationFrame()
    {
        //if we have another frame in the animation, set the current frame to the next one
        if (_currentAnimation.TryGetFrame(_currentAnimationFrame + 1, out var nextFrame))
        {
            SetAnimationFrame(_currentAnimationFrame + 1);
            return;
        }
        AnimationEnded(); //end the animation as we have no more frames
    }

    private void AnimationEnded()
    {
        //if we are a one shot
        if (_currentAnimation.OneShot)
        {
            //load next animation if we have one queued
            if (_animationQueue.Count > 0)
            {
                Animation = null;
                Animation = _animationQueue.Dequeue();
            }
            else
                Pause(); //or else just pause at the end of the oneshot.
        }
        else if(!_currentAnimation.OneShot)
        {
            SetAnimationFrame(0); //reset and loop.
        }
    }

    public void QueueAnimation(SpriteSheetAnimation animation)
    {
        _animationQueue.Enqueue(animation);
    }

    public void ClearAnimationQueue()
    {
        _animationQueue.Clear();
    }

    public void Pause()
    {
        IsPlaying = false;
    }

    public void Stop()
    {
        IsPlaying = false;
        ResetInternals();
    }

    public void Play()
    {
        IsPlaying = true;
    }

    public void ResetAndPlay()
    {
        Stop();
        Play();
    }

    private void SetAnimationFrame(int frameNumber)
    {
        if (_currentAnimation.TryGetFrame(frameNumber, out var nextFrame))
        {
            _currentAnimationFrame = frameNumber;
            _spriteDrawer.CurrentFrameIndex = nextFrame.FrameIndex;
            _spriteDrawer.Pivot = nextFrame.Pivot;
            
            if(nextFrame.AnimationEvent != null)
                _logger.Debug("Animation event was triggered: {0}", nextFrame.AnimationEvent.Name);
        }
    }

    private void UpdateAnimation(SpriteSheetAnimation newAnimation)
    {
        _currentAnimation = newAnimation;
        
        if(newAnimation == null)
            return;
        
        _spriteDrawer.SpriteSheetPath = newAnimation.SpriteSheetPath;
        
        SetFrameRate(newAnimation.FrameRate);
        ResetInternals();
        SetAnimationFrame(0);
    }

    private void ResetInternals()
    {
        _elapsedFrameTime = 0;
    }

    private void SetFrameRate(int newFrameRate)
    {
        _timeToNextFrame = 1 / (float)newFrameRate;
    }

    public override void OnDestroyed()
    {
        _spriteDrawer = null;
        _currentAnimation = null;
        _animationQueue.Clear();
        _animationQueue = null;
    }
}