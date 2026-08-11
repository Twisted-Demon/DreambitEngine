using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;

namespace Dreambit.Scripting;

internal static class ScriptFactory
{
    internal static ScriptAction CreateScript(CutsceneAction definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var scriptType = ResolveScriptType(definition.Script);
        var errors = new List<string>();

        foreach (var constructor in scriptType.GetConstructors().OrderByDescending(c => c.GetParameters().Length))
        {
            if (!TryCreateArguments(constructor, definition.Arguments, out var values, out var error))
            {
                errors.Add(error);
                continue;
            }

            try
            {
                return (ScriptAction)constructor.Invoke(values);
            }
            catch (TargetInvocationException exception)
            {
                throw new InvalidDataException(
                    $"The constructor for script '{definition.Script}' failed.",
                    exception.InnerException ?? exception);
            }
        }

        var details = errors.Count == 0
            ? "The type has no public constructors."
            : string.Join(" ", errors.Distinct());
        throw new InvalidDataException(
            $"No constructor for script '{definition.Script}' matches the cutscene arguments. {details}");
    }

    private static Type ResolveScriptType(string name)
    {
        var candidates = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .Where(type =>
                !type.IsAbstract &&
                typeof(ScriptAction).IsAssignableFrom(type) &&
                (type.FullName?.Equals(name, StringComparison.Ordinal) == true ||
                 type.Name.Equals(name, StringComparison.Ordinal)))
            .Distinct()
            .ToArray();

        if (candidates.Length == 0)
            throw new InvalidDataException(
                $"Script action '{name}' was not found. It must derive from {nameof(ScriptAction)}.");

        var exactMatch = candidates.FirstOrDefault(type => type.FullName == name);
        if (exactMatch is not null)
            return exactMatch;

        if (candidates.Length == 1)
            return candidates[0];

        throw new InvalidDataException(
            $"Script action name '{name}' is ambiguous. Use a fully-qualified type name. Matches: " +
            string.Join(", ", candidates.Select(type => type.FullName)));
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null)!;
        }
    }

    private static bool TryCreateArguments(
        ConstructorInfo constructor,
        IReadOnlyDictionary<string, object> source,
        out object[] values,
        out string error)
    {
        var parameters = constructor.GetParameters();
        values = new object[parameters.Length];
        var parameterNames = new HashSet<string>(
            parameters.Select(parameter => parameter.Name!),
            StringComparer.OrdinalIgnoreCase);
        var unknownArguments = source.Keys.Where(key => !parameterNames.Contains(key)).ToArray();

        if (unknownArguments.Length != 0)
        {
            error = $"Unknown argument(s): {string.Join(", ", unknownArguments)}.";
            return false;
        }

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (!source.TryGetValue(parameter.Name!, out var rawValue))
            {
                if (!parameter.IsOptional)
                {
                    error = $"Missing required argument '{parameter.Name}'.";
                    return false;
                }

                values[i] = parameter.DefaultValue;
                continue;
            }

            try
            {
                values[i] = ConvertToExpectedType(rawValue, parameter.ParameterType);
            }
            catch (Exception exception)
            {
                error = $"Argument '{parameter.Name}' is invalid: {exception.Message}";
                return false;
            }
        }

        error = null;
        return true;
    }

    private static object ConvertToExpectedType(object value, Type targetType)
    {
        var nullableType = Nullable.GetUnderlyingType(targetType);
        if (value is null)
        {
            if (!targetType.IsValueType || nullableType is not null)
                return null;
            throw new InvalidCastException($"{targetType.Name} cannot be null.");
        }

        targetType = nullableType ?? targetType;
        if (targetType == typeof(object) || targetType.IsInstanceOfType(value))
            return value;

        if (targetType == typeof(Vector2))
        {
            var values = GetSequence(value, "Vector2");
            if (values.Count != 2)
                throw new FormatException("Expected [x, y].");
            return new Vector2(ToSingle(values[0]), ToSingle(values[1]));
        }

        if (targetType == typeof(Vector3))
        {
            var values = GetSequence(value, "Vector3");
            if (values.Count != 3)
                throw new FormatException("Expected [x, y, z].");
            return new Vector3(ToSingle(values[0]), ToSingle(values[1]), ToSingle(values[2]));
        }

        if (targetType.IsArray)
        {
            var sourceValues = GetSequence(value, targetType.Name);
            var elementType = targetType.GetElementType()!;
            var array = Array.CreateInstance(elementType, sourceValues.Count);
            for (var i = 0; i < sourceValues.Count; i++)
                array.SetValue(ConvertToExpectedType(sourceValues[i], elementType), i);
            return array;
        }

        if (targetType.IsEnum)
            return value is string enumName
                ? Enum.Parse(targetType, enumName, true)
                : Enum.ToObject(targetType, value);

        if (targetType == typeof(string))
            return value.ToString();

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static IList GetSequence(object value, string targetName)
    {
        if (value is IList list)
            return list;
        throw new InvalidCastException($"{targetName} requires a YAML sequence.");
    }

    private static float ToSingle(object value)
    {
        return Convert.ToSingle(value, CultureInfo.InvariantCulture);
    }
}
