using System.Collections.Generic;

namespace PixelariaEngine.ECS;

public class DrawableList
{
    private readonly List<DrawableComponent> _drawables = [];
    private readonly Dictionary<int, List<DrawableComponent>> _drawablesByDrawLayer = [];

    internal int Count => _drawables.Count;
    internal DrawableComponent this[int index] => _drawables[index];

    internal void Add(DrawableComponent drawable)
    {
        if (_drawables.Contains(drawable)) return;

        _drawables.Add(drawable);
        AddToDrawLayer(drawable, drawable.DrawLayer);
    }

    internal void Remove(DrawableComponent drawable)
    {
        _drawables.Remove(drawable);
        _drawablesByDrawLayer[drawable.DrawLayer].Remove(drawable);
    }

    internal void ClearLists()
    {
        _drawables.Clear();
        _drawablesByDrawLayer.Clear();
    }

    internal void UpdateDrawableDrawLayer(DrawableComponent drawable, int oldLayer, int newLayer)
    {
        var oldLayerList = GetDrawablesByDrawLayer(oldLayer);
        oldLayerList.Remove(drawable);

        var newLayerList = GetDrawablesByDrawLayer(newLayer);
        newLayerList.Add(drawable);
    }

    private void AddToDrawLayer(DrawableComponent drawable, int drawLayer)
    {
        var list = GetDrawablesByDrawLayer(drawLayer);

        if (list.Contains(drawable))
            return;

        list.Add(drawable);
    }

    public List<DrawableComponent> GetDrawablesByDrawLayer(int drawLayer)
    {
        if (!_drawablesByDrawLayer.TryGetValue(drawLayer, out _))
            _drawablesByDrawLayer[drawLayer] = [];

        return _drawablesByDrawLayer[drawLayer];
    }
    
    public List<DrawableComponent> GetAllDrawables() => _drawables;
}