using System;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

public class UiButton : UiText
{
    private string _spritePath;
    private Sprite _sprite;
    private bool _fitToTexture;
    private bool _pressStartedInside;

    public event Action<UiButton> Clicked;

    public bool IsHovered { get; private set; }
    public bool IsPressed { get; private set; }
    public Color BackgroundTint { get; set; } = Color.White;
    public Color HoverTint { get; set; } = Color.LightGray;
    public Color PressedTint { get; set; } = Color.Gray;

    public string SpritePath
    {
        get => _spritePath;
        set
        {
            var next = value ?? string.Empty;
            if (_spritePath == next) return;

            _spritePath = next;
            InvalidateDependencies();
            InvalidateLayout();
        }
    }

    public UiButton()
    {
        AutoResizeHeight = false;
        MultiLine = false;
    }

    public override void Arrange(Rectangle parentBounds)
    {
        if (_fitToTexture && _sprite is not null)
        {
            Width = UiLength.Pixels(_sprite.SourceRect.Width);
            Height = UiLength.Pixels(_sprite.SourceRect.Height);
        }

        base.Arrange(parentBounds);
    }

    public override void ResolveDependencies()
    {
        base.ResolveDependencies();

        _sprite = string.IsNullOrEmpty(_spritePath)
            ? null
            : Resources.LoadAsset<Sprite>(_spritePath);
    }

    protected override void OnUpdate(in UiInputState input)
    {
        base.OnUpdate(input);

        IsHovered = input.PointerInWindow &&
                    Bounds.Contains(input.PointerPosition.ToPoint());

        if (input.PrimaryPressed)
            _pressStartedInside = IsHovered;

        IsPressed = _pressStartedInside && input.PrimaryHeld;

        if (!input.PrimaryReleased)
            return;

        if (_pressStartedInside && IsHovered)
            Clicked?.Invoke(this);

        _pressStartedInside = false;
        IsPressed = false;
    }

    public override void OnDraw()
    {
        if (_sprite is not null)
        {
            var tint = IsPressed
                ? PressedTint
                : IsHovered
                    ? HoverTint
                    : BackgroundTint;

            Graphics.SpriteBatch.Draw(
                _sprite.Texture,
                Bounds,
                _sprite.SourceRect,
                tint);
        }

        base.OnDraw();
    }

    public override void Parse(XmlNode node)
    {
        base.Parse(node);

        SpritePath = ParseString(node, "sprite", string.Empty);
        _fitToTexture = ParseBool(node, "fit-to-texture", false);

        if (node.Attributes?["background-tint"] is not null)
            BackgroundTint = ParseColor(node, "background-tint");

        if (node.Attributes?["hover-tint"] is not null)
            HoverTint = ParseColor(node, "hover-tint");

        if (node.Attributes?["pressed-tint"] is not null)
            PressedTint = ParseColor(node, "pressed-tint");
    }
}
