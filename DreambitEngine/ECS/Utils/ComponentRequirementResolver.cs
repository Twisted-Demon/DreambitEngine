using System;
using System.Collections.Generic;
using System.Linq;

namespace Dreambit.ECS;

public static class ComponentRequirementResolver
{
    private enum Mark : byte { None, Visiting, Done }

    public static IReadOnlyList<Type> ResolveOrder(
        Type root,
        Func<Type, bool> hasAlready // e.g., entity.HasComponentOfType
    )
    {
        var order = new List<Type>(8);
        var mark  = new Dictionary<Type, Mark>(16);
        var stack = new Stack<Type>(); // for pretty cycle messages

        void Visit(Type t)
        {
            if (mark.TryGetValue(t, out var m))
            {
                if (m == Mark.Visiting)
                {
                    // Build a readable cycle chain
                    var cycle = string.Join(" -> ", stack.Reverse().Append(t).Select(x => x.FullName));
                    throw new InvalidOperationException($"Cycle detected in [Require]: {cycle}");
                }
                // m == Done → nothing to do
                return;
            }

            mark[t] = Mark.Visiting;
            stack.Push(t);

            foreach (var req in GetRequireTypes(t))
                Visit(req);

            stack.Pop();
            mark[t] = Mark.Done;

            // We only need to attach if the entity doesn't have it yet
            if (!hasAlready(t))
                order.Add(t);
        }

        Visit(root);

        // We want dependencies first, so reverse the postorder
        order.Reverse();

        // If you resolve for the "root" type you’re adding *right now*,
        // you usually don’t want to include root itself here—Entity.AttachComponent(root)
        // is what triggers the setup. Remove it if present.
        if (order.Count > 0 && order[^1] == root)
            order.RemoveAt(order.Count - 1);

        return order;
    }

    private static IEnumerable<Type> GetRequireTypes(Type t)
    {
        // Gather all [Require] from the type (and optionally base classes if you want)
        foreach (var attr in t.GetCustomAttributes(inherit: true))
        {
            if (attr is RequireAttribute r)
            {
                foreach (var rt in r.RequiredTypes)
                    yield return rt;
            }
        }
    }
}