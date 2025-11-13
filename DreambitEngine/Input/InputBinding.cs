using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Dreambit;

public struct InputBinding
{
    public readonly BindingType BindingType;

    public Keys Key { get; private set; }
    public MouseButton MouseButton { get; private set; }

    public Axis1D Axis { get; private set; }
    public float Scale { get; private set; }

    public  Keys Up { get; private set; }
    public  Keys Down { get; private set; }
    public  Keys Left { get; private set; }
    public  Keys Right { get; private set; }
    

    public  Keys ModKey { get; private set; }
    public  Keys PrimaryKey { get; private set; }

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
        new(BindingType.Axis1D) { Axis = axis, Scale = scale == 0 ? 1f : scale };
    
    public static InputBinding Composite2DType(Keys up, Keys down, Keys left, Keys right) =>
        new(BindingType.Composite2D) { Up = up, Down = down, Left = left, Right = right };
    
    public static InputBinding ChordType(Keys modifier, Keys primary) =>
        new(BindingType.Chord) { ModKey = modifier, PrimaryKey = primary };
    
    public bool IsPressed() =>
        BindingType switch
        {
            BindingType.Key => Input.IsKeyPressed(Key),
            BindingType.MouseButton => Input.IsMousePressed(MouseButton),
            BindingType.Chord => Input.IsKeyPressed(ModKey) && Input.IsKeyPressed(PrimaryKey),
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
                switch (Axis)
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
            float x = 0f, y = 0f;
            if (Input.IsKeyHeld(Right)) x += 1f;
            if (Input.IsKeyHeld(Left))  x -= 1f;
            if (Input.IsKeyHeld(Up))    y -= 1f;
            if (Input.IsKeyHeld(Down))  y += 1f;
            if (x != 0 || y != 0)
            {
                var v = new Vector2(x, y);
                var len = v.Length();
                return len > 1e-5f ? v / len : Vector2.Zero; // normalize for consistent speed
            }
        }
        return Vector2.Zero;
    }
}

public enum BindingType
{
    Key = 0,
    MouseButton = 1,
    Axis1D = 2,
    Composite2D = 3,
    Chord = 4,
}