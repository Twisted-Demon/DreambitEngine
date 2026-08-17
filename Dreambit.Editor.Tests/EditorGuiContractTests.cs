using System.Numerics;
using System.Reflection;
using Dreambit.EditorApi;

namespace Dreambit.Editor.Tests;

public sealed class EditorGuiContractTests
{
    [Fact]
    public void TypedPropertyOverloadsRequireExplicitIdAndLabelParameters()
    {
        var propertyMethods = typeof(EditorGui)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == nameof(EditorGui.Property))
            .ToArray();
        Type[] supportedValueTypes =
        [
            typeof(bool),
            typeof(int),
            typeof(float),
            typeof(double),
            typeof(string),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4)
        ];

        foreach (var valueType in supportedValueTypes)
        {
            var method = Assert.Single(propertyMethods, candidate =>
            {
                var parameters = candidate.GetParameters();
                return parameters.Length >= 3 &&
                       parameters[2].ParameterType == valueType.MakeByRefType();
            });
            var parameters = method.GetParameters();

            AssertParameter(parameters[0], "id", typeof(string));
            AssertParameter(parameters[1], "label", typeof(string));
        }
    }

    [Fact]
    public void SectionRequiresAnExplicitIdAndExposesItsState()
    {
        var method = Assert.Single(
            typeof(EditorGui).GetMethods(BindingFlags.Public | BindingFlags.Static),
            candidate => candidate.Name == nameof(EditorGui.Section));
        var parameters = method.GetParameters();

        AssertParameter(parameters[0], "id", typeof(string));
        AssertParameter(parameters[1], "title", typeof(string));
        Assert.Equal(typeof(EditorGuiSectionScope), method.ReturnType);
        Assert.NotNull(typeof(EditorGuiSectionScope).GetProperty(nameof(EditorGuiSectionScope.IsOpen)));
        Assert.NotNull(typeof(EditorGuiSectionScope).GetProperty(nameof(EditorGuiSectionScope.RemoveRequested)));
    }

    [Fact]
    public void SemanticThemeTokensAreValidAndDistinct()
    {
        Vector4[] colors =
        [
            EditorGuiTheme.WindowBackground,
            EditorGuiTheme.PanelBackground,
            EditorGuiTheme.SurfaceBackground,
            EditorGuiTheme.SelectedBackground,
            EditorGuiTheme.PrimaryAccent,
            EditorGuiTheme.SecondaryAccent,
            EditorGuiTheme.PrimaryText,
            EditorGuiTheme.MutedText,
            EditorGuiTheme.DisabledText,
            EditorGuiTheme.Border
        ];

        Assert.All(colors, color =>
        {
            Assert.InRange(color.X, 0f, 1f);
            Assert.InRange(color.Y, 0f, 1f);
            Assert.InRange(color.Z, 0f, 1f);
            Assert.InRange(color.W, 0f, 1f);
        });
        Assert.NotEqual(EditorGuiTheme.WindowBackground, EditorGuiTheme.PanelBackground);
        Assert.NotEqual(EditorGuiTheme.PrimaryAccent, EditorGuiTheme.SecondaryAccent);
        Assert.True(EditorGuiTheme.ControlHeight > 0f);
        Assert.True(EditorGuiTheme.PropertyLabelWidth >= EditorGuiTheme.MinimumPropertyLabelWidth);
        Assert.InRange(EditorGuiTheme.PropertyLabelRatio, 0.01f, 0.99f);
    }

    private static void AssertParameter(ParameterInfo parameter, string name, Type type)
    {
        Assert.Equal(name, parameter.Name);
        Assert.Equal(type, parameter.ParameterType);
    }
}
