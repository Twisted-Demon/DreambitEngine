using Dreambit.Editor.Assets;
using Dreambit.EditorApi;

namespace Dreambit.Editor.Inspection;

internal sealed class AssetInspector(
    InspectorMetadataCache metadata,
    InspectorValueDrawerRegistry drawers,
    BlueprintInspector blueprints,
    CustomInspectorHost customInspectors)
{
    public void Draw(DreambitAssetDocument document)
    {
        EditorGui.Header(
            document.Asset.Name + (document.IsDirty ? " *" : string.Empty),
            document.Asset.RelativePath);

        if (customInspectors.TryDraw(
                document.AssetType,
                [document.Instance],
                () => DrawDefault(document),
                (name, mutation) => document.Apply(name, _ => mutation()),
                $"Custom asset Editor for '{document.AssetType.FullName}' failed."))
        {
            return;
        }

        DrawDefault(document);
    }

    private void DrawDefault(DreambitAssetDocument document)
    {
        if (document.Instance is EntityBlueprint blueprint)
        {
            blueprints.Draw(document, blueprint);
            return;
        }

        foreach (var member in metadata.Get(document.AssetType, InspectorTargetKind.Asset))
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(member.Header))
                    EditorGui.Header(member.Header);

                var value = member.GetValue(document.Instance);
                var result = drawers.Draw(
                    member.DisplayName,
                    member.ValueType,
                    value,
                    new InspectorValueDrawContext(
                        $"Asset.{document.Asset.Id}.{member.SerializedName}",
                        member,
                        false,
                        member.IsReadOnly));
                if (result.Changed && !member.IsReadOnly)
                    document.Apply(
                        $"Change {member.DisplayName}",
                        asset => member.SetValue(asset, result.Value),
                        $"Asset.{document.Asset.Id}.{member.SerializedName}");
            }
            catch (Exception exception)
            {
                EditorGui.Error($"{member.DisplayName}: {exception.Message}");
            }
        }
    }
}
