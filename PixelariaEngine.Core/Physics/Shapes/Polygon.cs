using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PixelariaEngine;

public struct Polygon
{
    public Vector2[] Vertices;
    public int Length => Vertices.Length;

    public Vector2 this[int key]
    {
        get => Vertices[key];
        set => Vertices[key] = value;
    }

    public Vector2[] GetEdges()
    {
        var edges = new Vector2[Vertices.Length];
        for (var i = 0; i < Vertices.Length; i++)
        {
            var current = Vertices[i];
            var next = Vertices[(i + 1) % Vertices.Length];
            edges[i] = next - current;
        }

        return edges;
    }
    
    public (float min, float max) ProjectOntoAxis(Vector2 axis)
    {
        var min = Vector2.Dot(Vertices[0], axis);
        var max = min;

        for (var i = 1; i < Vertices.Length; i++)
        {
            float projection = Vector2.Dot(Vertices[i], axis);
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

    public Polygon Transform(Matrix transformationMatrix)
    {
        var polygon = new Polygon
        {
            Vertices = new Vector2[Length]
        };

        for (var i = 0; i < Length; i++)
        {
            var point3D = new Vector3(Vertices[i], 0);
            var transformedPoint = Vector3.Transform(point3D, transformationMatrix);
            
            polygon.Vertices[i] = new Vector2(transformedPoint.X, transformedPoint.Y);
        }

        return polygon;
    }
}