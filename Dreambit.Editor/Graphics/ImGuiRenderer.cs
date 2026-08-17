using System.Numerics;
using System.Runtime.InteropServices;
using Dreambit.EditorApi;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Vector2 = System.Numerics.Vector2;

namespace Dreambit.Editor.Graphics;

internal sealed class ImGuiRenderer : IDisposable
{
    private const float MouseWheelDelta = 120f;
    private const int ImDrawVertexSize = 20;

    private static readonly VertexDeclaration ImDrawVertexDeclaration = new(
        ImDrawVertexSize,
        new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
        new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0));

    private readonly Game _game;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly string _layoutPath;
    private readonly nint _context;
    private readonly RasterizerState _rasterizerState;
    private readonly Dictionary<nint, Texture2D> _textures = [];
    private readonly Keys[] _allKeys = Enum.GetValues<Keys>();

    private BasicEffect? _effect;
    private DynamicVertexBuffer? _vertexBuffer;
    private DynamicIndexBuffer? _indexBuffer;
    private byte[] _vertexData = [];
    private byte[] _indexData = [];
    private int _vertexBufferSize;
    private int _indexBufferSize;
    private int _nextTextureId = 1;
    private nint _fontTextureId;
    private Texture2D? _fontTexture;
    private bool _fontAtlasConfigured;
    private int _scrollWheelValue;
    private int _horizontalScrollWheelValue;
    private bool _disposed;

    public unsafe ImGuiRenderer(Game game, string layoutPath)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _graphicsDevice = game.GraphicsDevice;
        _layoutPath = layoutPath;

        _context = ImGui.CreateContext();
        ImGui.SetCurrentContext(_context);

        var io = ImGui.GetIO();
        io.ConfigFlags |=
            ImGuiConfigFlags.DockingEnable |
            ImGuiConfigFlags.NavEnableKeyboard;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.ConfigWindowsMoveFromTitleBarOnly = true;
        io.NativePtr->IniFilename = null;

        _rasterizerState = new RasterizerState
        {
            CullMode = CullMode.None,
            FillMode = FillMode.Solid,
            ScissorTestEnable = true,
            MultiSampleAntiAlias = false
        };

        _game.Window.TextInput += OnTextInput;
        EditorGui.ApplyTheme();
        RebuildFontAtlas();

        if (File.Exists(_layoutPath))
            ImGui.LoadIniSettingsFromDisk(_layoutPath);
    }

    public bool HasSavedLayout => File.Exists(_layoutPath);

    public nint BindTexture(Texture2D texture)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(texture);

        var id = (nint)_nextTextureId++;
        _textures.Add(id, texture);
        return id;
    }

    public bool UnbindTexture(nint textureId) => _textures.Remove(textureId);

    public unsafe void RebuildFontAtlas()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ImGui.SetCurrentContext(_context);

        var io = ImGui.GetIO();
        ConfigureFontAtlas(io);
        io.Fonts.GetTexDataAsRGBA32(
            out byte* pixelData,
            out var width,
            out var height,
            out var bytesPerPixel);

        var pixels = new byte[checked(width * height * bytesPerPixel)];
        Marshal.Copy((nint)pixelData, pixels, 0, pixels.Length);

        if (_fontTextureId != 0)
            UnbindTexture(_fontTextureId);
        _fontTexture?.Dispose();

        _fontTexture = new Texture2D(
            _graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.Color)
        {
            Name = "Dreambit Editor ImGui font atlas"
        };
        _fontTexture.SetData(pixels);
        _fontTextureId = BindTexture(_fontTexture);
        io.Fonts.SetTexID(_fontTextureId);
        io.Fonts.ClearTexData();
    }

    private void ConfigureFontAtlas(ImGuiIOPtr io)
    {
        if (_fontAtlasConfigured)
            return;

        io.Fonts.Clear();
        var fontPath = FindEditorFont();
        if (fontPath is null)
            io.Fonts.AddFontDefault();
        else
            io.Fonts.AddFontFromFileTTF(fontPath, EditorGuiTheme.FontSize);

        _fontAtlasConfigured = true;
    }

    private static string? FindEditorFont()
    {
        var bundledInter = Path.Combine(
            AppContext.BaseDirectory,
            "Fonts",
            "Inter-VariableFont_opsz,wght.ttf");
        if (File.Exists(bundledInter))
            return bundledInter;

        string[] candidates =
        [
            @"C:\Windows\Fonts\segoeui.ttf",
            "/System/Library/Fonts/Supplemental/Arial.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
        ];

        foreach (var candidate in candidates)
            if (File.Exists(candidate))
                return candidate;

        return null;
    }

    public void BeginLayout(GameTime gameTime)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ImGui.SetCurrentContext(_context);

        var io = ImGui.GetIO();
        io.DeltaTime = MathF.Max((float)gameTime.ElapsedGameTime.TotalSeconds, 1f / 1_000f);
        UpdateInput(io);
        ImGui.NewFrame();
    }

    public void EndLayout()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ImGui.SetCurrentContext(_context);
        ImGui.Render();
        RenderDrawData(ImGui.GetDrawData());

        var io = ImGui.GetIO();
        if (!io.WantSaveIniSettings)
            return;

        SaveLayout();
        io.WantSaveIniSettings = false;
    }

    public void SaveLayout()
    {
        if (_disposed)
            return;

        ImGui.SetCurrentContext(_context);
        var directory = Path.GetDirectoryName(_layoutPath);
        if (string.IsNullOrWhiteSpace(directory))
            return;

        Directory.CreateDirectory(directory);
        ImGui.SaveIniSettingsToDisk(_layoutPath);
    }

    private void UpdateInput(ImGuiIOPtr io)
    {
        var clientBounds = _game.Window.ClientBounds;
        var clientWidth = Math.Max(1, clientBounds.Width);
        var clientHeight = Math.Max(1, clientBounds.Height);
        var backBufferWidth = Math.Max(
            1,
            _graphicsDevice.PresentationParameters.BackBufferWidth);
        var backBufferHeight = Math.Max(
            1,
            _graphicsDevice.PresentationParameters.BackBufferHeight);

        io.DisplaySize = new Vector2(clientWidth, clientHeight);
        io.DisplayFramebufferScale = new Vector2(
            backBufferWidth / (float)clientWidth,
            backBufferHeight / (float)clientHeight);
        io.AddFocusEvent(_game.IsActive);

        if (!_game.IsActive)
        {
            io.AddMousePosEvent(-float.MaxValue, -float.MaxValue);
            return;
        }

        var mouse = Mouse.GetState();
        io.AddMousePosEvent(mouse.X, mouse.Y);
        io.AddMouseButtonEvent(0, mouse.LeftButton == ButtonState.Pressed);
        io.AddMouseButtonEvent(1, mouse.RightButton == ButtonState.Pressed);
        io.AddMouseButtonEvent(2, mouse.MiddleButton == ButtonState.Pressed);
        io.AddMouseButtonEvent(3, mouse.XButton1 == ButtonState.Pressed);
        io.AddMouseButtonEvent(4, mouse.XButton2 == ButtonState.Pressed);
        io.AddMouseWheelEvent(
            (mouse.HorizontalScrollWheelValue - _horizontalScrollWheelValue) / MouseWheelDelta,
            (mouse.ScrollWheelValue - _scrollWheelValue) / MouseWheelDelta);
        _horizontalScrollWheelValue = mouse.HorizontalScrollWheelValue;
        _scrollWheelValue = mouse.ScrollWheelValue;

        var keyboard = Keyboard.GetState();
        foreach (var key in _allKeys)
        {
            if (TryMapKey(key, out var imGuiKey))
                io.AddKeyEvent(imGuiKey, keyboard.IsKeyDown(key));
        }

        io.AddKeyEvent(
            ImGuiKey.ModCtrl,
            keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl));
        io.AddKeyEvent(
            ImGuiKey.ModShift,
            keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift));
        io.AddKeyEvent(
            ImGuiKey.ModAlt,
            keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt));
        io.AddKeyEvent(
            ImGuiKey.ModSuper,
            keyboard.IsKeyDown(Keys.LeftWindows) || keyboard.IsKeyDown(Keys.RightWindows));
    }

    private void RenderDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.TotalVtxCount <= 0 || drawData.TotalIdxCount <= 0)
            return;

        EnsureBuffers(drawData);
        CopyDrawDataToBuffers(drawData);

        var previousViewport = _graphicsDevice.Viewport;
        var previousScissor = _graphicsDevice.ScissorRectangle;
        var previousRasterizer = _graphicsDevice.RasterizerState;
        var previousDepthStencil = _graphicsDevice.DepthStencilState;
        var previousBlendState = _graphicsDevice.BlendState;
        var previousBlendFactor = _graphicsDevice.BlendFactor;
        var previousSampler = _graphicsDevice.SamplerStates[0];

        try
        {
            _graphicsDevice.BlendFactor = Microsoft.Xna.Framework.Color.White;
            _graphicsDevice.BlendState = BlendState.NonPremultiplied;
            _graphicsDevice.RasterizerState = _rasterizerState;
            _graphicsDevice.DepthStencilState = DepthStencilState.None;
            _graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
            _graphicsDevice.Viewport = new Viewport(
                0,
                0,
                _graphicsDevice.PresentationParameters.BackBufferWidth,
                _graphicsDevice.PresentationParameters.BackBufferHeight);
            _graphicsDevice.SetVertexBuffer(_vertexBuffer);
            _graphicsDevice.Indices = _indexBuffer;

            RenderCommandLists(drawData);
        }
        finally
        {
            _graphicsDevice.Viewport = previousViewport;
            _graphicsDevice.ScissorRectangle = previousScissor;
            _graphicsDevice.RasterizerState = previousRasterizer;
            _graphicsDevice.DepthStencilState = previousDepthStencil;
            _graphicsDevice.BlendState = previousBlendState;
            _graphicsDevice.BlendFactor = previousBlendFactor;
            _graphicsDevice.SamplerStates[0] = previousSampler;
            _graphicsDevice.SetVertexBuffer(null);
            _graphicsDevice.Indices = null;
        }
    }

    private void EnsureBuffers(ImDrawDataPtr drawData)
    {
        if (drawData.TotalVtxCount > _vertexBufferSize)
        {
            _vertexBuffer?.Dispose();
            _vertexBufferSize = Math.Max(
                drawData.TotalVtxCount,
                (int)(drawData.TotalVtxCount * 1.5f));
            _vertexBuffer = new DynamicVertexBuffer(
                _graphicsDevice,
                ImDrawVertexDeclaration,
                _vertexBufferSize,
                BufferUsage.None);
            _vertexData = new byte[checked(_vertexBufferSize * ImDrawVertexSize)];
        }

        if (drawData.TotalIdxCount > _indexBufferSize)
        {
            _indexBuffer?.Dispose();
            _indexBufferSize = Math.Max(
                drawData.TotalIdxCount,
                (int)(drawData.TotalIdxCount * 1.5f));
            _indexBuffer = new DynamicIndexBuffer(
                _graphicsDevice,
                IndexElementSize.SixteenBits,
                _indexBufferSize,
                BufferUsage.None);
            _indexData = new byte[checked(_indexBufferSize * sizeof(ushort))];
        }
    }

    private unsafe void CopyDrawDataToBuffers(ImDrawDataPtr drawData)
    {
        var vertexOffset = 0;
        var indexOffset = 0;

        for (var listIndex = 0; listIndex < drawData.CmdListsCount; listIndex++)
        {
            var commandList = drawData.CmdLists[listIndex];
            var vertexByteOffset = vertexOffset * ImDrawVertexSize;
            var indexByteOffset = indexOffset * sizeof(ushort);
            var vertexByteCount = commandList.VtxBuffer.Size * ImDrawVertexSize;
            var indexByteCount = commandList.IdxBuffer.Size * sizeof(ushort);

            fixed (byte* vertexDestination = &_vertexData[vertexByteOffset])
            fixed (byte* indexDestination = &_indexData[indexByteOffset])
            {
                Buffer.MemoryCopy(
                    (void*)commandList.VtxBuffer.Data,
                    vertexDestination,
                    _vertexData.Length - vertexByteOffset,
                    vertexByteCount);
                Buffer.MemoryCopy(
                    (void*)commandList.IdxBuffer.Data,
                    indexDestination,
                    _indexData.Length - indexByteOffset,
                    indexByteCount);
            }

            vertexOffset += commandList.VtxBuffer.Size;
            indexOffset += commandList.IdxBuffer.Size;
        }

        _vertexBuffer!.SetData(
            _vertexData,
            0,
            drawData.TotalVtxCount * ImDrawVertexSize,
            SetDataOptions.Discard);
        _indexBuffer!.SetData(
            _indexData,
            0,
            drawData.TotalIdxCount * sizeof(ushort),
            SetDataOptions.Discard);
    }

    private void RenderCommandLists(ImDrawDataPtr drawData)
    {
        var globalVertexOffset = 0;
        var globalIndexOffset = 0;
        var frameBufferScale = drawData.FramebufferScale;
        var displayPosition = drawData.DisplayPos;
        var frameBufferWidth = _graphicsDevice.PresentationParameters.BackBufferWidth;
        var frameBufferHeight = _graphicsDevice.PresentationParameters.BackBufferHeight;

        for (var listIndex = 0; listIndex < drawData.CmdListsCount; listIndex++)
        {
            var commandList = drawData.CmdLists[listIndex];
            for (var commandIndex = 0; commandIndex < commandList.CmdBuffer.Size; commandIndex++)
            {
                var command = commandList.CmdBuffer[commandIndex];
                if (command.ElemCount == 0 || command.UserCallback != 0)
                    continue;

                if (!_textures.TryGetValue(command.TextureId, out var texture))
                    throw new InvalidOperationException(
                        $"ImGui requested unknown texture id '{command.TextureId}'.");

                var clipMinimum = new Vector2(
                    (command.ClipRect.X - displayPosition.X) * frameBufferScale.X,
                    (command.ClipRect.Y - displayPosition.Y) * frameBufferScale.Y);
                var clipMaximum = new Vector2(
                    (command.ClipRect.Z - displayPosition.X) * frameBufferScale.X,
                    (command.ClipRect.W - displayPosition.Y) * frameBufferScale.Y);

                var left = Math.Clamp((int)clipMinimum.X, 0, frameBufferWidth);
                var top = Math.Clamp((int)clipMinimum.Y, 0, frameBufferHeight);
                var right = Math.Clamp((int)MathF.Ceiling(clipMaximum.X), 0, frameBufferWidth);
                var bottom = Math.Clamp((int)MathF.Ceiling(clipMaximum.Y), 0, frameBufferHeight);
                if (right <= left || bottom <= top)
                    continue;

                _graphicsDevice.ScissorRectangle = new Rectangle(
                    left,
                    top,
                    right - left,
                    bottom - top);

                var effect = UpdateEffect(texture, drawData);
                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
#pragma warning disable CS0618
                    _graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        (int)command.VtxOffset + globalVertexOffset,
                        0,
                        commandList.VtxBuffer.Size,
                        (int)command.IdxOffset + globalIndexOffset,
                        (int)command.ElemCount / 3);
#pragma warning restore CS0618
                }
            }

            globalVertexOffset += commandList.VtxBuffer.Size;
            globalIndexOffset += commandList.IdxBuffer.Size;
        }
    }

    private BasicEffect UpdateEffect(Texture2D texture, ImDrawDataPtr drawData)
    {
        _effect ??= new BasicEffect(_graphicsDevice)
        {
            TextureEnabled = true,
            VertexColorEnabled = true,
            World = Matrix.Identity,
            View = Matrix.Identity
        };

        _effect.Texture = texture;
        _effect.Projection = Matrix.CreateOrthographicOffCenter(
            drawData.DisplayPos.X,
            drawData.DisplayPos.X + drawData.DisplaySize.X,
            drawData.DisplayPos.Y + drawData.DisplaySize.Y,
            drawData.DisplayPos.Y,
            -1f,
            1f);
        return _effect;
    }

    private static bool TryMapKey(Keys key, out ImGuiKey imGuiKey)
    {
        imGuiKey = key switch
        {
            Keys.Back => ImGuiKey.Backspace,
            Keys.Tab => ImGuiKey.Tab,
            Keys.Enter => ImGuiKey.Enter,
            Keys.CapsLock => ImGuiKey.CapsLock,
            Keys.Escape => ImGuiKey.Escape,
            Keys.Space => ImGuiKey.Space,
            Keys.PageUp => ImGuiKey.PageUp,
            Keys.PageDown => ImGuiKey.PageDown,
            Keys.End => ImGuiKey.End,
            Keys.Home => ImGuiKey.Home,
            Keys.Left => ImGuiKey.LeftArrow,
            Keys.Up => ImGuiKey.UpArrow,
            Keys.Right => ImGuiKey.RightArrow,
            Keys.Down => ImGuiKey.DownArrow,
            Keys.PrintScreen => ImGuiKey.PrintScreen,
            Keys.Insert => ImGuiKey.Insert,
            Keys.Delete => ImGuiKey.Delete,
            >= Keys.D0 and <= Keys.D9 =>
                (ImGuiKey)((int)ImGuiKey._0 + ((int)key - (int)Keys.D0)),
            >= Keys.A and <= Keys.Z =>
                (ImGuiKey)((int)ImGuiKey.A + ((int)key - (int)Keys.A)),
            >= Keys.NumPad0 and <= Keys.NumPad9 =>
                (ImGuiKey)((int)ImGuiKey.Keypad0 + ((int)key - (int)Keys.NumPad0)),
            Keys.Multiply => ImGuiKey.KeypadMultiply,
            Keys.Add => ImGuiKey.KeypadAdd,
            Keys.Subtract => ImGuiKey.KeypadSubtract,
            Keys.Decimal => ImGuiKey.KeypadDecimal,
            Keys.Divide => ImGuiKey.KeypadDivide,
            >= Keys.F1 and <= Keys.F24 =>
                (ImGuiKey)((int)ImGuiKey.F1 + ((int)key - (int)Keys.F1)),
            Keys.NumLock => ImGuiKey.NumLock,
            Keys.Scroll => ImGuiKey.ScrollLock,
            Keys.LeftShift => ImGuiKey.LeftShift,
            Keys.RightShift => ImGuiKey.RightShift,
            Keys.LeftControl => ImGuiKey.LeftCtrl,
            Keys.RightControl => ImGuiKey.RightCtrl,
            Keys.LeftAlt => ImGuiKey.LeftAlt,
            Keys.RightAlt => ImGuiKey.RightAlt,
            Keys.LeftWindows => ImGuiKey.LeftSuper,
            Keys.RightWindows => ImGuiKey.RightSuper,
            Keys.OemSemicolon => ImGuiKey.Semicolon,
            Keys.OemPlus => ImGuiKey.Equal,
            Keys.OemComma => ImGuiKey.Comma,
            Keys.OemMinus => ImGuiKey.Minus,
            Keys.OemPeriod => ImGuiKey.Period,
            Keys.OemQuestion => ImGuiKey.Slash,
            Keys.OemTilde => ImGuiKey.GraveAccent,
            Keys.OemOpenBrackets => ImGuiKey.LeftBracket,
            Keys.OemCloseBrackets => ImGuiKey.RightBracket,
            Keys.OemPipe => ImGuiKey.Backslash,
            Keys.OemQuotes => ImGuiKey.Apostrophe,
            Keys.BrowserBack => ImGuiKey.AppBack,
            Keys.BrowserForward => ImGuiKey.AppForward,
            _ => ImGuiKey.None
        };

        return imGuiKey != ImGuiKey.None;
    }

    private static bool IsTextControlCharacter(char character) =>
        character is '\t' or '\r' or '\n';

    private void OnTextInput(object? sender, TextInputEventArgs args)
    {
        if (_disposed || IsTextControlCharacter(args.Character))
            return;

        ImGui.SetCurrentContext(_context);
        ImGui.GetIO().AddInputCharacter(args.Character);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        ImGui.SetCurrentContext(_context);
        _game.Window.TextInput -= OnTextInput;
        _textures.Clear();
        _fontTexture?.Dispose();
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _effect?.Dispose();
        _rasterizerState.Dispose();
        ImGui.DestroyContext(_context);
        _disposed = true;
    }
}
