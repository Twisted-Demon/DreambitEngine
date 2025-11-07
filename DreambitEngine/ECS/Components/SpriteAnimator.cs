using System;
using System.Collections.Generic;

namespace Dreambit.ECS;

[Require(typeof(SpriteDrawer))]
public class SpriteAnimator : Component
{
    private readonly Logger<SpriteAnimator> _logger = new();
    private Queue<SpriteSheetAnimation> _animationQueue = [];
    private int _currentAnimationFrame;
    private float _elapsedFrameTime;
    private readonly Dictionary<string, Action> _eventActions = [];

    //internals
    private SpriteDrawer _spriteDrawer;
    private float _timeToNextFrame;
    public Action OnAnimationEnded;
    public bool IsPlaying { get; private set; }

    public SpriteSheetAnimation Animation { get; private set; }

    private string AnimationPath { get; set; }
    
    public override void OnCreated()
    {
        _spriteDrawer = Entity.GetComponent<SpriteDrawer>();
        _spriteDrawer.WithPivot(PivotType.Custom);
    }

    public override void OnUpdate()
    {
        if (Animation == null)
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
        if (Animation.TryGetFrame(_currentAnimationFrame + 1, out var nextFrame))
        {
            SetAnimationFrame(_currentAnimationFrame + 1);
            return;
        }

        AnimationEnded(); //end the animation as we have no more frames
    }

    private void AnimationEnded()
    {
        //if we are a one shot
        if (Animation.OneShot)
        {
            OnAnimationEnded?.Invoke();

            //load next animation if we have one queued
            if (_animationQueue.Count > 0)
            {
                Animation = null;
                Animation = _animationQueue.Dequeue();
            }
            else
            {
                Pause(); //or else just pause at the end of the oneshot.
            }
        }
        else
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

    public void SetAnimation(string animationPath)
    {
        if (AnimationPath == animationPath)
            return;
        
        AnimationPath = animationPath;
        UpdateAnimation(animationPath);
    }
    
    public void RegisterEvent(string eventName, Action eventAction)
    {
        if (!_eventActions.TryAdd(eventName, null))
        {
            // Add the event action to the existing one (+= syntax allows you to chain multiple methods to the same event)
            _eventActions[eventName] += eventAction;
        }
        else
        {
            // If the event doesn't exist, create a new one
            _eventActions[eventName] += eventAction;
        }
    }

    public void DeregisterEvent(string eventName)
    {
        if (_eventActions.TryGetValue(eventName, out var eventAction))
            _eventActions[eventName] -= eventAction;
    }

    private void SetAnimationFrame(int frameNumber)
    {
        if (Animation.TryGetFrame(frameNumber, out var nextFrame))
        {
            _currentAnimationFrame = frameNumber;
            _spriteDrawer.SetFrame(nextFrame.FrameIndex);
            _spriteDrawer.WithPivot(nextFrame.Pivot);

            if (nextFrame.AnimationEvent == null) return;

            if (_eventActions.TryGetValue(nextFrame.AnimationEvent.Name, out var eventAction))
                eventAction?.Invoke();
        }
    }

    private void UpdateAnimation(string animPath)
    {
        var newAnimation = Resources.LoadAsset<SpriteSheetAnimation>(animPath);
        
        Animation = newAnimation;

        if (newAnimation == null)
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
        Animation = null;
        _animationQueue.Clear();
        _animationQueue = null;
    }
}