using System.Collections.Generic;
using System.Linq;

namespace Dreambit.ECS;

public class LightingList
{
    private const int ComponentsSize = 32;

    // -------- backing storage --------
    private readonly List<Light2D> _all = new(ComponentsSize);
    private readonly Dictionary<Light2D, int> _allIdx = new(ComponentsSize);


    internal int Count => _all.Count;

    internal void Add(Light2D light)
    {
        if (_allIdx.ContainsKey(light)) return;

        // add to "all"
        _allIdx[light] = _all.Count;
        _all.Add(light);
    }

    internal void ClearLists()
    {
        _all.Clear();
        _allIdx.Clear();
    }

    internal void Remove(Light2D light)
    {
        if (!_allIdx.TryGetValue(light, out _)) return;

        // remove from "all"
        SwapRemove(_all, _allIdx, light);
    }

    public List<Light2D> GetAllLights()
    {
        return _all;
    }

    public List<Light2D> GetAllEnabledLights()
    {
        return GetAllLights().Where(IsEnabled).ToList();
    }

    private static bool IsEnabled(Light2D light)
    {
        return light.Enabled && light.Entity.Enabled;
    }

    private static void SwapRemove<T>(List<T> list, Dictionary<T, int> indexMap, T item)
    {
        if (!indexMap.TryGetValue(item, out var idx)) return;
        var last = list.Count - 1;

        if (idx != last)
        {
            var lastItem = list[last];
            list[idx] = lastItem;
            indexMap[lastItem] = idx;
        }

        list.RemoveAt(last);
        indexMap.Remove(item);
    }
}