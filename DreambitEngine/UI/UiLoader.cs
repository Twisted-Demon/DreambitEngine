using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Dreambit.UI;

/// <summary>
/// Creates UI layouts from XML using element and brush types discovered in
/// all currently loaded assemblies.
/// </summary>
public static class UiLoader
{
    /// <summary>Parses a complete UI XML document into a retained visual tree.</summary>
    /// <param name="xml">XML containing a single <c>&lt;Ui&gt;</c> root.</param>
    /// <returns>The parsed layout.</returns>
    /// <exception cref="XmlException">
    /// Thrown when the document, element types, brush types, properties, or IDs
    /// are invalid or ambiguous.
    /// </exception>
    public static UiLayout LoadFromXml(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var rootNode = doc.SelectSingleNode("/Ui");
        if (rootNode is null)
            throw new XmlException("UI document must contain a <Ui> root.");

        var typeCatalog = new UiTypeCatalog();
        var rootPanel = new UiPanel
        {
            Id = "root",
            X = UiLength.Pixels(0),
            Y = UiLength.Pixels(0),
            Width = UiLength.Percent(1f),
            Height = UiLength.Percent(1f),
            Anchor = UiAnchor.TopLeft,
            Origin = UiAnchor.TopLeft
        };

        foreach (XmlNode child in rootNode.ChildNodes)
        {
            if (child.NodeType != XmlNodeType.Element)
                continue;

            rootPanel.AddChild(
                ParseElement(child, rootPanel, typeCatalog));
        }

        ValidateUniqueIds(rootPanel, []);

        return new UiLayout
        {
            Root = rootPanel
        };
    }

    private static UiElement ParseElement(
        XmlNode node,
        UiContainer parent,
        UiTypeCatalog typeCatalog)
    {
        var element = typeCatalog.CreateElement(node.Name);
        element.Parent = parent;
        element.ParseInternal(node);

        if (element is UiContainer container)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.NodeType != XmlNodeType.Element)
                    continue;

                if (TryParsePropertyElement(
                        element,
                        node.Name,
                        childNode,
                        typeCatalog))
                {
                    continue;
                }

                container.AddChild(
                    ParseElement(childNode, container, typeCatalog));
            }
        }

        return element;
    }

    private static bool TryParsePropertyElement(
        UiElement element,
        string elementName,
        XmlNode propertyNode,
        UiTypeCatalog typeCatalog)
    {
        var backgroundPropertyName = $"{elementName}.Background";
        if (propertyNode.Name != backgroundPropertyName)
            return false;

        if (element is not UiContentControl contentControl)
        {
            throw new XmlException(
                $"<{backgroundPropertyName}> is only valid on a content control.");
        }

        if (contentControl.Background is not null)
        {
            throw new XmlException(
                $"<{backgroundPropertyName}> can only be specified once.");
        }

        contentControl.Background = ParseBrush(propertyNode, typeCatalog);
        return true;
    }

    private static IUiBrush ParseBrush(
        XmlNode propertyNode,
        UiTypeCatalog typeCatalog)
    {
        XmlNode brushNode = null;

        foreach (XmlNode childNode in propertyNode.ChildNodes)
        {
            if (childNode.NodeType != XmlNodeType.Element)
                continue;

            if (brushNode is not null)
            {
                throw new XmlException(
                    $"<{propertyNode.Name}> must contain exactly one brush.");
            }

            brushNode = childNode;
        }

        if (brushNode is null)
        {
            throw new XmlException(
                $"<{propertyNode.Name}> must contain exactly one brush.");
        }

        var brush = typeCatalog.CreateBrush(brushNode.Name);
        brush.Parse(brushNode);
        return brush;
    }

    private static IList<IUiBrush> ParseBrushes(
        XmlNode propertyNode,
        UiTypeCatalog typeCatalog)
    {
        IList<IUiBrush> result = [];
        
        foreach (XmlNode childNode in propertyNode.ChildNodes)
        {
            if (childNode.NodeType != XmlNodeType.Element)
                continue;
            
            var brush = typeCatalog.CreateBrush(childNode.Name);
            brush.Parse(childNode);

            result.Add(brush);
        }

        return result;
    }

    private static void ValidateUniqueIds(
        UiElement element,
        HashSet<string> ids)
    {
        if (!string.IsNullOrWhiteSpace(element.Id) &&
            !ids.Add(element.Id))
        {
            throw new XmlException(
                $"Duplicate UI element id '{element.Id}'.");
        }

        foreach (var child in element.Children)
            ValidateUniqueIds(child, ids);
    }

    private sealed class UiTypeCatalog
    {
        private readonly Dictionary<string, List<Type>> _elementTypes =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<Type>> _brushTypes =
            new(StringComparer.Ordinal);

        public UiTypeCatalog()
        {
            var assemblies = AppDomain.CurrentDomain
                .GetAssemblies()
                .OrderBy(assembly => assembly.FullName, StringComparer.Ordinal);

            foreach (var assembly in assemblies)
            {
                foreach (var type in GetLoadableTypes(assembly))
                {
                    if (type.IsAbstract ||
                        type.IsInterface ||
                        type.ContainsGenericParameters)
                    {
                        continue;
                    }

                    if (typeof(UiElement).IsAssignableFrom(type))
                    {
                        AddType(
                            _elementTypes,
                            GetElementXmlName(type),
                            type);
                    }

                    if (typeof(IUiBrush).IsAssignableFrom(type))
                    {
                        AddType(
                            _brushTypes,
                            GetBrushXmlName(type),
                            type);
                    }
                }
            }
        }

        public UiElement CreateElement(string xmlName)
        {
            return (UiElement)CreateInstance(
                ResolveType(_elementTypes, xmlName, "element"),
                xmlName,
                "element");
        }

        public IUiBrush CreateBrush(string xmlName)
        {
            return (IUiBrush)CreateInstance(
                ResolveType(_brushTypes, xmlName, "brush"),
                xmlName,
                "brush");
        }

        private static Type ResolveType(
            IReadOnlyDictionary<string, List<Type>> types,
            string xmlName,
            string kind)
        {
            if (!types.TryGetValue(xmlName, out var matches) ||
                matches.Count == 0)
            {
                throw new XmlException(
                    $"Unsupported UI {kind} <{xmlName}>. " +
                    "The type must be present in a loaded assembly.");
            }

            if (matches.Count > 1)
            {
                var names = string.Join(
                    ", ",
                    matches.Select(type => type.AssemblyQualifiedName));
                throw new XmlException(
                    $"UI {kind} <{xmlName}> is ambiguous. Matches: {names}.");
            }

            return matches[0];
        }

        private static object CreateInstance(
            Type type,
            string xmlName,
            string kind)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor is null)
            {
                throw new XmlException(
                    $"Could not create UI {kind} <{xmlName}> from " +
                    $"{type.FullName}. UI types require a public parameterless " +
                    "constructor.");
            }

            try
            {
                return constructor.Invoke(null);
            }
            catch (TargetInvocationException exception)
            {
                throw new XmlException(
                    $"The constructor for UI {kind} <{xmlName}> " +
                    $"({type.FullName}) threw an exception.",
                    exception.InnerException ?? exception);
            }
        }

        private static string GetElementXmlName(Type type)
        {
            var explicitName = type
                .GetCustomAttribute<UiXmlNameAttribute>()
                ?.Name;
            if (!string.IsNullOrEmpty(explicitName))
                return explicitName;

            return type.Name.StartsWith("Ui", StringComparison.Ordinal) &&
                   type.Name.Length > 2
                ? type.Name[2..]
                : type.Name;
        }

        private static string GetBrushXmlName(Type type)
        {
            return type
                       .GetCustomAttribute<UiXmlNameAttribute>()
                       ?.Name ??
                   type.Name;
        }

        private static void AddType(
            IDictionary<string, List<Type>> types,
            string xmlName,
            Type type)
        {
            if (!types.TryGetValue(xmlName, out var matches))
            {
                matches = [];
                types.Add(xmlName, matches);
            }

            matches.Add(type);
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.OfType<Type>();
            }
            catch (NotSupportedException)
            {
                return [];
            }
        }
    }
}
