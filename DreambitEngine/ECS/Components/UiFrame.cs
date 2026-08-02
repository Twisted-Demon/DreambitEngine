using System;
using System.IO;
using Dreambit.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Dreambit.ECS;

[BlueprintType(nameof(UiFrame))]
public class UiFrame : DrawableComponent<UiFrame>
{
    private string _layoutPath;

    public string LayoutPath
    {
        get => _layoutPath;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value);

            if (_layoutPath == value)
                return;
            
            _layoutPath = value;
            LoadLayout(_layoutPath);
        }
    }
    public UiLayout Layout { get; private set; }

    public void LoadLayout(string layoutPath)
    {
        if (string.IsNullOrWhiteSpace(layoutPath))
            throw new ArgumentException(
                "A UI layout path is required.",
                nameof(layoutPath));

        LayoutPath = layoutPath;

        var contentRoot = Path.Combine(
            AppContext.BaseDirectory,
            Core.Instance.Content.RootDirectory);

        var fullPath = Path.GetFullPath(
            Path.Combine(contentRoot, LayoutPath));

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"UI layout '{LayoutPath}' was not found.",
                fullPath);
        }

        Layout = UiLoader.LoadFromXml(
            File.ReadAllText(fullPath));
    }

    public UiFrame WithLayout(string layoutPath)
    {
        LoadLayout(layoutPath);

        return this;
    }

    internal UiInputCapture RouteInput(UiInputCapture availableInput)
    {
        if (Layout is null)
            return UiInputCapture.None;

        var uiScale = MathF.Max(
            Scene.UiCamera.Scale,
            0.0001f);

        var viewport = new Rectangle(
            0,
            0,
            (int)MathF.Ceiling(Window.Width / uiScale),
            (int)MathF.Ceiling(Window.Height / uiScale));

        var backBufferPointer = Window.ClientToBackBuffer(
            Input.GetMousePosition());

        var uiPointer = Scene.UiCamera.CameraLocalToWorld(
            backBufferPointer);

        var pointerAvailable = availableInput.HasFlag(UiInputCapture.Pointer);
        var keyboardAvailable = availableInput.HasFlag(UiInputCapture.Keyboard);
        var gamePadAvailable = availableInput.HasFlag(UiInputCapture.GamePad);
        var navigationDirection = GetNavigationDirection(
            keyboardAvailable,
            gamePadAvailable,
            out var navigationDevice);
        var tabPressed = keyboardAvailable &&
                         Input.IsRawKeyPressed(Keys.Tab);

        var input = new UiInputState(
            uiPointer,
            pointerAvailable && Input.IsMouseInWindow(),
            pointerAvailable && Input.IsRawMousePressed(MouseButton.Left),
            pointerAvailable && Input.IsRawMouseHeld(MouseButton.Left),
            pointerAvailable && Input.IsRawMouseReleased(MouseButton.Left),
            pointerAvailable ? Input.GetRawScrollDelta() : 0,
            keyboardAvailable ? Input.GetRawPressedKeys() : [],
            keyboardAvailable ? Input.GetRawReleasedKeys() : [],
            navigationDirection,
            navigationDevice,
            tabPressed && !Input.IsRawShiftDown(),
            tabPressed && Input.IsRawShiftDown(),
            keyboardAvailable &&
            (Input.IsRawKeyPressed(Keys.Enter) ||
             Input.IsRawKeyPressed(Keys.Space)),
            gamePadAvailable && Input.IsRawGamePadButtonPressed(Buttons.A),
            keyboardAvailable && Input.IsRawKeyPressed(Keys.Escape),
            gamePadAvailable && Input.IsRawGamePadButtonPressed(Buttons.B),
            keyboardAvailable && IsKeyboardNavigationHeld(),
            gamePadAvailable && IsGamePadNavigationHeld());

        return Layout.Update(viewport, input);
    }

    public override void OnDrawUi()
    {
        Layout?.Draw(Scene.UiCamera.TopLeftTransformMatrix);
    }

    public override RectangleF Bounds => Scene.UiCamera.BoundsF;

    private static UiNavigationDirection? GetNavigationDirection(
        bool keyboardAvailable,
        bool gamePadAvailable,
        out UiInputDevice device)
    {
        if (keyboardAvailable)
        {
            if (Input.IsRawKeyPressed(Keys.Left))
                return SetDirection(UiNavigationDirection.Left, out device);
            if (Input.IsRawKeyPressed(Keys.Right))
                return SetDirection(UiNavigationDirection.Right, out device);
            if (Input.IsRawKeyPressed(Keys.Up))
                return SetDirection(UiNavigationDirection.Up, out device);
            if (Input.IsRawKeyPressed(Keys.Down))
                return SetDirection(UiNavigationDirection.Down, out device);
        }

        if (gamePadAvailable)
        {
            if (Input.IsRawGamePadButtonPressed(Buttons.DPadLeft) ||
                Input.IsRawLeftStickDirectionPressed(UiNavigationDirection.Left))
            {
                return SetDirection(UiNavigationDirection.Left, out device, true);
            }

            if (Input.IsRawGamePadButtonPressed(Buttons.DPadRight) ||
                Input.IsRawLeftStickDirectionPressed(UiNavigationDirection.Right))
            {
                return SetDirection(UiNavigationDirection.Right, out device, true);
            }

            if (Input.IsRawGamePadButtonPressed(Buttons.DPadUp) ||
                Input.IsRawLeftStickDirectionPressed(UiNavigationDirection.Up))
            {
                return SetDirection(UiNavigationDirection.Up, out device, true);
            }

            if (Input.IsRawGamePadButtonPressed(Buttons.DPadDown) ||
                Input.IsRawLeftStickDirectionPressed(UiNavigationDirection.Down))
            {
                return SetDirection(UiNavigationDirection.Down, out device, true);
            }
        }

        device = UiInputDevice.None;
        return null;
    }

    private static UiNavigationDirection SetDirection(
        UiNavigationDirection direction,
        out UiInputDevice device,
        bool gamePad = false)
    {
        device = gamePad
            ? UiInputDevice.GamePad
            : UiInputDevice.Keyboard;
        return direction;
    }

    private static bool IsKeyboardNavigationHeld()
    {
        return Input.IsRawKeyHeld(Keys.Tab) ||
               Input.IsRawKeyHeld(Keys.Left) ||
               Input.IsRawKeyHeld(Keys.Right) ||
               Input.IsRawKeyHeld(Keys.Up) ||
               Input.IsRawKeyHeld(Keys.Down) ||
               Input.IsRawKeyHeld(Keys.Enter) ||
               Input.IsRawKeyHeld(Keys.Space) ||
               Input.IsRawKeyHeld(Keys.Escape);
    }

    private static bool IsGamePadNavigationHeld()
    {
        var stick = Input.GetRawGamePadLeftStick();
        return Input.IsRawGamePadButtonHeld(Buttons.DPadLeft) ||
               Input.IsRawGamePadButtonHeld(Buttons.DPadRight) ||
               Input.IsRawGamePadButtonHeld(Buttons.DPadUp) ||
               Input.IsRawGamePadButtonHeld(Buttons.DPadDown) ||
               Input.IsRawGamePadButtonHeld(Buttons.A) ||
               Input.IsRawGamePadButtonHeld(Buttons.B) ||
               MathF.Abs(stick.X) >= 0.5f ||
               MathF.Abs(stick.Y) >= 0.5f;
    }
}
