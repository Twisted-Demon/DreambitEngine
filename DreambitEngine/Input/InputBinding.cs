using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Dreambit;

public struct InputBinding
{
    public readonly BindingType BindingType;

    public Keys Key { get; init; }
    public MouseButton MouseButton { get;  init; }

    public Axis1D Axis1D { get; init; }
    public Axis2D Axis2D { get;  init; }
    public float Scale { get;  init; }

    public  Keys Up { get; init; }
    public  Keys Down { get;  init; }
    public  Keys Left { get; init; }
    public  Keys Right { get; init; }
    

    public  Keys ModKey { get; set; }
    public  Keys PrimaryKey { get; set; }

    private InputBinding(BindingType bindingType)
    {
        this = default;
        BindingType = bindingType;
    }
    
    public static InputBinding KeyType(Keys k) =>
        new(BindingType.Key) { Key = k };

    public static InputBinding MouseType(MouseButton btn) =>
        new(BindingType.MouseButton) { MouseButton = btn };

    public static InputBinding AxisType(Axis1D axis, float scale = 1f) =>
        new(BindingType.Axis1D) { Axis1D = axis, Scale = scale == 0 ? 1f : scale };
    
    public static InputBinding Composite2DKeys(Keys up, Keys down, Keys left, Keys right) =>
        new(BindingType.Composite2D) { Up = up, Down = down, Left = left, Right = right, Axis2D = Axis2D.Keys};

    public static InputBinding Composite2DMouse() =>
        new(BindingType.Composite2D) { Axis2D = Axis2D.Mouse };
    
    public static InputBinding ChordType(Keys modifier, Keys primary) =>
        new(BindingType.Chord) { ModKey = modifier, PrimaryKey = primary };
    
    public bool IsPressed() =>
        BindingType switch
        {
            BindingType.Key => Input.IsKeyPressed(Key),
            BindingType.MouseButton => Input.IsMousePressed(MouseButton),
            BindingType.Chord => Input.IsKeyHeld(ModKey) && Input.IsKeyPressed(PrimaryKey),
            _ => false
        };

    public bool IsHeld() =>
        BindingType switch
        {
            BindingType.Key => Input.IsKeyHeld(Key),
            BindingType.MouseButton => Input.IsMouseHeld(MouseButton),
            BindingType.Chord => Input.IsKeyHeld(ModKey) && Input.IsKeyHeld(PrimaryKey),
            _ => false
        };
    
    public bool IsReleased() =>
        BindingType switch
        {
            BindingType.Key => Input.IsKeyReleased(Key),
            BindingType.MouseButton => Input.IsMouseReleased(MouseButton),
            BindingType.Chord => Input.IsKeyReleased(ModKey) && Input.IsKeyReleased(PrimaryKey),
            _ => false
        };

    public float Read1D()
    {
        switch (BindingType)
        {
            case Dreambit.BindingType.Axis1D:
                switch (Axis1D)
                {
                    case Axis1D.MouseX: return Input.GetMouseDelta().X * Scale;
                    case Axis1D.MouseY: return Input.GetMouseDelta().Y * Scale;
                    case Axis1D.Scroll: return Input.GetScrollDelta() *  Scale;
                }
                break;
            case Dreambit.BindingType.Key:
                return Input.IsKeyHeld(Key) ? 1f * Scale : 0f;
            case Dreambit.BindingType.MouseButton:
                return Input.IsMouseHeld(MouseButton) ? 1f * Scale : 0f;
        }

        return 0f;
    }
    
    public Vector2 Read2D()
    {
        if (BindingType == BindingType.Composite2D)
        {
            switch (Axis2D)
            {
                case Axis2D.Mouse:
                    var delta = Input.GetMouseDelta();
                    if(delta != Vector2.Zero)
                        delta.Normalize();
                    
                    return delta;
                case Axis2D.Joystick:
                    break;
                case Axis2D.Keys:
                    float x = 0f, y = 0f;
                    if (Input.IsKeyHeld(Right)) x += 1f;
                    if (Input.IsKeyHeld(Left))  x -= 1f;
                    if (Input.IsKeyHeld(Up))    y -= 1f;
                    if (Input.IsKeyHeld(Down))  y += 1f;
                    
                    var axis = new Vector2(x, y);
                    if(axis != Vector2.Zero)
                        axis.Normalize();
                    
                    return axis;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        return Vector2.Zero;
    }
}