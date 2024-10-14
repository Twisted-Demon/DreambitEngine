using System.Collections.Generic;
using System.Linq;

namespace PixelariaEngine.ECS;

public class DrawableList
{
    private List<DrawableComponent> _drawables = [];
    private Dictionary<int, List<DrawableComponent>> _drawLayers = [];

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
        _drawLayers[drawable.DrawLayer].Remove(drawable);
    }

    internal void ClearLists()
    {
        _drawables.Clear();
        _drawLayers.Clear();

        _drawables = null;
        _drawLayers = null;
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
        if (!_drawLayers.TryGetValue(drawLayer, out _))
            _drawLayers[drawLayer] = [];

        return _drawLayers[drawLayer];
    }
    
    public int DrawLayerCount => _drawLayers.Count;

    public Dictionary<int, List<DrawableComponent>> GetDrawLayers()
    {
        return _drawLayers;
    }

    public List<DrawableComponent> GetAllDrawables()
    {
        return _drawables;
    }

    public List<DrawableComponent> GetAllEnabledDrawables()
    {
        return _drawables.Where(d => d.Enabled && d.Entity.Enabled).ToList();
    }
}