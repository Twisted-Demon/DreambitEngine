using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Dreambit.ECS;

public sealed class DrawableRepository
{
    private const int MaxComponents = 4096;
    
    // ---------- Backing storage ----------
    private readonly List<DrawableComponent> _all = new(MaxComponents);
    private readonly Dictionary<DrawableComponent, int> _allIdx = new(MaxComponents);
    
    // Per-layer buckets 
    private readonly Dictionary<int, List<DrawableComponent>> _byLayer = new(MaxComponents);
    private readonly Dictionary<int, Dictionary<DrawableComponent, int>> _byLayerIdx = new(MaxComponents);
    
    // per-type buckets
    private readonly Dictionary<Type, IList> _byType = new(MaxComponents);
    
    // ---------- Public surface (same as your original + fast paths) ----------
    internal int Count => _all.Count;
    internal DrawableComponent this[int index] => _all[index];
    public int DrawLayerCount => _byLayer.Count;

    internal void Add(DrawableComponent drawable)
    {
        if (_allIdx.ContainsKey(drawable)) return;

        // Add to "all"
        _allIdx[drawable] = _all.Count;
        _all.Add(drawable);

        // Add to layer buckets
        AddToLayer(drawable, drawable.DrawLayer);

        var t = drawable.GetType();
        
        if (!_byType.TryGetValue(t, out var typeList))
            _byType[t] = typeList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(t))!;
        
        typeList.Add(drawable);
    }

    internal void Remove(DrawableComponent drawable)
    {
        if (!_allIdx.TryGetValue(drawable, out _)) return;

        // Remove from "all"
        SwapRemove(_all, _allIdx, drawable);

        // Remove from layer buckets
        RemoveFromLayer(drawable, drawable.DrawLayer);
        
        var t = drawable.GetType();

        if (!_byType.TryGetValue(t, out var typeList))
            return;

        typeList.Remove(drawable);
    }
    
    internal void ClearLists()
    {
        _all.Clear(); _allIdx.Clear();

        foreach (var kv in _byLayer) kv.Value.Clear();
        foreach (var kv in _byLayerIdx) kv.Value.Clear();

        _byLayer.Clear(); _byLayerIdx.Clear();
    }

    internal void UpdateDrawableDrawLayer(DrawableComponent drawable, int oldLayer, int newLayer)
    {
        if (!_allIdx.ContainsKey(drawable)) return;
        if (oldLayer == newLayer) return;

        // Move in "all" layer buckets
        RemoveFromLayer(drawable, oldLayer);
        AddToLayer(drawable, newLayer);
        
    }

    private void AddToLayer(DrawableComponent d, int layer)
    {
        if (!_byLayer.TryGetValue(layer, out var list))
        {
            list = new List<DrawableComponent>(128);
            _byLayer[layer] = list;
            _byLayerIdx[layer] = new Dictionary<DrawableComponent, int>(128);
        }
        _byLayerIdx[layer][d] = list.Count;
        list.Add(d);
    }

    private void RemoveFromLayer(DrawableComponent d, int layer)
    {
        if (_byLayer.TryGetValue(layer, out var list) && _byLayerIdx.TryGetValue(layer, out var idxMap))
        {
            SwapRemove(list, idxMap, d);
            if (list.Count == 0) { _byLayer.Remove(layer); _byLayerIdx.Remove(layer); }
        }
    }
    

    // ---------- Original API helpers (kept) ----------
    public IReadOnlyList<DrawableComponent> GetDrawablesByDrawLayer(int drawLayer)
    {
        if (!_byLayer.TryGetValue(drawLayer, out var list))
        {
            list = new List<DrawableComponent>(0);
            _byLayer[drawLayer] = list;
            _byLayerIdx[drawLayer] = new Dictionary<DrawableComponent, int>(0);
        }
        return list;
    }

    public Dictionary<int, List<DrawableComponent>> GetDrawLayers() => _byLayer;

    public IReadOnlyList<DrawableComponent> GetAllDrawables() => _all;

    public IReadOnlyList<DrawableComponent> GetAllEnabledDrawables()
    {
        return GetAllDrawables().Where(IsEnabled).ToList();
    }

    public IReadOnlyList<T> GetAllDrawablesByType<T>() where T : DrawableComponent
    {
        if (_byType.TryGetValue(typeof(T), out var list))
            return (IReadOnlyList<T>)list;

        return new List<T>(0);
    }

    // ---------- Internals ----------
    private static bool IsEnabled(DrawableComponent d) => d.Enabled && d.Entity.Enabled;

    private static void SwapRemove<T>(List<T> list, Dictionary<T, int> indexMap, T item)
    {
        if (!indexMap.TryGetValue(item, out int idx)) return;
        int last = list.Count - 1;

        if (idx != last)
        {
            T lastItem = list[last];
            list[idx] = lastItem;
            indexMap[lastItem] = idx;
        }

        list.RemoveAt(last);
        indexMap.Remove(item);
    }
}