namespace Dreambit.Editor.UI.Panels;

internal interface IEditorPanel : IDisposable
{
    string Id { get; }
    string Title { get; }
    string WindowName { get; }
    bool IsOpen { get; set; }

    void Draw();
}
