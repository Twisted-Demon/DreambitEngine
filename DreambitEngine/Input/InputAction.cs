using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Dreambit;

public sealed class InputAction
{
    public string Name { get; set; }
    public InputActionType Type { get; set; }
    public float DeadZone { get; set; } = 0.001f;
    public bool Enabled { get; private set; } = true;

    private readonly List<InputBinding> _bindings = new(4);
    private InputActionPhase _phase = InputActionPhase.Waiting;
    private float _time;
    private float _v1;
    private Vector2 _v2;
    
    public event Action<InputActionContext> Started;
    public event Action<InputActionContext> Performed;
    public event Action<InputActionContext> Canceled;

    public InputAction(string name, InputActionType type)
    {
        Name = name;
        Type = type;
    }

    public InputAction AddBinding(InputBinding binding)
    {
        _bindings.Add(binding);
        return this;
    }
    
    public void Enable() => Enabled = true;
    public void Disable() => Enabled = false;

    internal void Update()
    {
        if (!Enabled) return;

        _time += Time.DeltaTime;

        switch (Type)
        {
            case InputActionType.Button:
            {
                bool pressed = false, held = false, released = false;
                for (var i = 0; i < _bindings.Count; i++)
                {
                    pressed |= _bindings[i].IsPressed();
                    held |= _bindings[i].IsHeld();
                    released |= _bindings[i].IsReleased();
                }
                
                if (_phase == InputActionPhase.Waiting && pressed)
                {
                    _phase = InputActionPhase.Started; _time = 0;
                    Started?.Invoke(new(Name, _time, 1f, default));
                    _phase = InputActionPhase.Performed;
                    Performed?.Invoke(new(Name, _time, 1f, default));
                }
                else if (_phase == InputActionPhase.Performed && !held && released)
                {
                    _phase = InputActionPhase.Canceled; _time = 0;
                    Canceled?.Invoke(new(Name, _time, 0f, default));
                    _phase = InputActionPhase.Waiting; _time = 0;
                }
                else if (_phase == InputActionPhase.Performed && held)
                {
                    Performed?.Invoke(new(Name, _time, 1f, default)); // fire each frame while held (like Unity's performed)
                }
                
                break;
            }
            case InputActionType.Value1D:
            {
                var v = 0f;
                for (var i = 0; i < _bindings.Count; i++) v += _bindings[i].Read1D();
                if(Mathf.Abs(v) < DeadZone) v = 0f;
                
                var wasZero = Mathf.Abs(_v1) < DeadZone;
                var isZero = Mathf.Abs(v) < DeadZone;

                if (wasZero && !isZero)
                {
                    _phase = InputActionPhase.Started; _time = 0; _v1 = v;
                    Started?.Invoke(new InputActionContext(Name, _time, _v1, default));
                    _phase = InputActionPhase.Performed;
                }

                if (!isZero)
                {
                    _v1 = v;
                    Performed?.Invoke(new InputActionContext(Name, _time, _v1, default));
                }
                else if (!wasZero && isZero)
                {
                    _v1 = 0f;
                    _phase = InputActionPhase.Canceled; _time = 0;
                    Canceled?.Invoke(new InputActionContext(Name, _time, 0f, default));
                    _phase = InputActionPhase.Waiting; _time = 0;
                }
                break;
            }
            case InputActionType.Value2D:
            {
                var v = Microsoft.Xna.Framework.Vector2.Zero;
                for (var i = 0; i < _bindings.Count; i++) v += _bindings[i].Read2D();
                if (v.LengthSquared() < DeadZone * DeadZone) v = Microsoft.Xna.Framework.Vector2.Zero;

                var wasZero = _v2.LengthSquared() < DeadZone * DeadZone;
                var isZero  = v.LengthSquared()   < DeadZone * DeadZone;

                if (wasZero && !isZero)
                {
                    _phase = InputActionPhase.Started; _time = 0; _v2 = v;
                    Started?.Invoke(new InputActionContext(Name, _time, 0f, _v2));
                    _phase = InputActionPhase.Performed;
                }

                if (!isZero)
                {
                    _v2 = v;
                    Performed?.Invoke(new InputActionContext(Name, _time, 0f, _v2));
                }
                else if (!wasZero && isZero)
                {
                    _v2 = Microsoft.Xna.Framework.Vector2.Zero;
                    _phase = InputActionPhase.Canceled; _time = 0;
                    Canceled?.Invoke(new InputActionContext(Name, _time, 0f, _v2));
                    _phase = InputActionPhase.Waiting; _time = 0;
                }
                break;
            }
        }
    }
}