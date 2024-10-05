using PixelariaEngine.Graphics;

namespace PixelariaEngine.ECS;

[Require(typeof(SpriteDrawer))]
public class AnimatedSprite : Component<AnimatedSprite>
{
    private SpriteDrawer _spriteDrawer;
    private SpriteSheetAnimation _spriteSheetAnimation;
    private string _animationPath;
    
    //internal variables
    private bool _isPlaying;
    private float _elapsedTime;
    private float _timeToNextFrame;
    private int _currentFrame;

    public string AnimationPath
    {
        get => _animationPath;
        set
        {
            if(_animationPath == value) return;
            _animationPath = value;
            SpriteSheetAnimation = Resources.Load<SpriteSheetAnimation>(value);
            OnAnimationPathChanged(value);
        }
    }
    
    public SpriteSheetAnimation SpriteSheetAnimation
    {
        get => _spriteSheetAnimation;
        set
        {
            if(_spriteSheetAnimation == value) return;
            _spriteSheetAnimation = value;
            _animationPath = _spriteSheetAnimation.AssetName;
            _spriteDrawer.SpriteSheetPath = _spriteSheetAnimation.SpriteSheetPath;
            OnSpriteSheetAnimationChanged(value);
        }
    }

    private void OnAnimationPathChanged(string newPath)
    {
        
    }

    private void OnSpriteSheetAnimationChanged(SpriteSheetAnimation newAnimation)
    {
        if(newAnimation == null) return;

        _timeToNextFrame = 1 / (float) newAnimation.FrameRate;
        _currentFrame = 0;
        _elapsedTime = 0;
        
        _spriteDrawer.CurrentFrameIndex = _currentFrame;
        _spriteDrawer.Origin = _spriteSheetAnimation[0].Pivot;
    }

    public override void OnCreated()
    {
        _spriteDrawer = Entity.GetComponent<SpriteDrawer>();
        _spriteDrawer.OriginType = SpriteOrigin.Custom;
    }

    public override void OnUpdate()
    {
        if (!_isPlaying) return;
        
        _elapsedTime += Time.DeltaTime; // increase the current time
        
        if(_elapsedTime >= _timeToNextFrame)
            ChangeFrame(); //change the frame if it's time
    }

    private void ChangeFrame()
    {
        _elapsedTime = 0; //reset the elapsed time

        _currentFrame++; // increment the frame
        
        if(_currentFrame >= SpriteSheetAnimation.FrameCount)
            _currentFrame = 0; //if we are at the frame count, reset
        
        _spriteDrawer.CurrentFrameIndex = _currentFrame; //set the sprite drawer to render the new frame
        _spriteDrawer.Origin = _spriteSheetAnimation[_currentFrame].Pivot;
    }

    public void Play()
    {
        _isPlaying = true;
    }

    public void Stop()
    {
        _isPlaying = false;
        _elapsedTime = 0;
        _timeToNextFrame = 0;
        _spriteDrawer.CurrentFrameIndex = 0;
    }

    public void Pause()
    {
        _isPlaying = false;
    }

    public void ResetAndPlay()
    {
        Stop();
        Play();
    }
}