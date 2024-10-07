using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PixelariaEngine;

public class Polygon
{
    public Vector2[] Corners { get; protected init; }

    protected Polygon(int cornerCount)
    {
        Corners = new Vector2[cornerCount];
    }

    public Vector2[] GetEdges()
    {
        var edges = new Vector2[Corners.Length];
        for (var i = 0; i < Corners.Length; i++)
        {
            var current = Corners[i];
            var next = Corners[(i + 1) % Corners.Length];
            edges[i] = next - current;
        }

        return edges;
    }

    public (float min, float max) ProjectOntoAxis(Vector2 axis)
    {
        var min = Vector2.Dot(Corners[0], axis);
        var max = min;

        for (var i = 1; i < Corners.Length; i++)
        {
            float projection = Vector2.Dot(Corners[i], axis);
            if(projection < min)
                min=projection;
            if(projection > max)
                max = projection;
        }

        return (min, max);
    }

    public bool Intersects(Polygon other)
    {
        var axes = new List<Vector2>();
        
        foreach(var edge in GetEdges())
            axes.Add(new Vector2(-edge.Y, edge.X));
        
        foreach(Vector2 edge in other.GetEdges())
            axes.Add(new Vector2(-edge.X, edge.Y));

        foreach (var axis in axes)
        {
            if(axis != Vector2.Zero)
                axis.Normalize();
            
            var(minA, maxA) = ProjectOntoAxis(axis);
            var(minB, maxB) = other.ProjectOntoAxis(axis);

            if (maxA < minB || maxB < minA)
                return false;
        }

        return true;
    }
}