using System.Runtime.CompilerServices;

namespace Dreambit.Editor;

/// <summary>
/// Runs user-defined cleanup in a frame that cannot keep collectible exceptions or object locals
/// alive through the assembly unload GC. Callers receive diagnostic text, never the exception.
/// </summary>
internal static class EditorDisposal
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string? TryDispose(IDisposable? disposable)
    {
        if (disposable is null)
            return null;
        string? failure = null;
        try
        {
            disposable.Dispose();
        }
        catch (Exception exception)
        {
            failure = exception.ToString();
        }

        return failure;
    }
}
