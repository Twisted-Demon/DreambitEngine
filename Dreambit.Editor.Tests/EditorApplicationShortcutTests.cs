namespace Dreambit.Editor.Tests;

public sealed class EditorApplicationShortcutTests
{
    [Fact]
    public void DocumentShortcutsAreAllowedWithoutTextInputFocus()
    {
        Assert.True(EditorApplication.ShouldHandleDocumentShortcut(wantTextInput: false));
    }

    [Fact]
    public void DocumentShortcutsAreSuppressedWhileImGuiOwnsTextInput()
    {
        Assert.False(EditorApplication.ShouldHandleDocumentShortcut(wantTextInput: true));
    }
}
