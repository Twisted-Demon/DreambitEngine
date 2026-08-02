using System;
using System.IO;
using Dreambit.UI;
using Microsoft.Xna.Framework;

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

    public override void OnUpdate()
    {
        if (Layout is null)
            return;

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

        var input = new UiInputState(
            uiPointer,
            Input.IsMouseInWindow(),
            Input.LeftPressed(),
            Input.LeftHeld(),
            Input.LeftReleased());

        Layout.Update(viewport, input);
    }

    public override void OnDrawUi()
    {
        Layout?.Draw();
    }

    public override RectangleF Bounds => Scene.UiCamera.BoundsF;
}
