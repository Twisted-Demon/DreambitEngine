using System.Runtime.InteropServices;
using ImGuiNET;

namespace Dreambit.Editor.UI;

internal static class ImGuiNativeDocking
{
    private const string CImGui = "cimgui";

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeVector2(float x, float y)
    {
        public readonly float X = x;
        public readonly float Y = y;
    }

    [DllImport(CImGui, EntryPoint = "igDockBuilderRemoveNode", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RemoveNode(uint nodeId);

    [DllImport(CImGui, EntryPoint = "igDockBuilderAddNode", CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint AddNode(uint nodeId, uint flags);

    [DllImport(CImGui, EntryPoint = "igDockBuilderSetNodeSize", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SetNodeSizeNative(uint nodeId, NativeVector2 size);

    internal static void SetNodeSize(uint nodeId, System.Numerics.Vector2 size) =>
        SetNodeSizeNative(nodeId, new NativeVector2(size.X, size.Y));

    [DllImport(CImGui, EntryPoint = "igDockBuilderSplitNode", CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint SplitNode(
        uint nodeId,
        ImGuiDir splitDirection,
        float sizeRatioForNodeAtDirection,
        out uint idAtDirection,
        out uint idAtOppositeDirection);

    [DllImport(CImGui, EntryPoint = "igDockBuilderDockWindow", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void DockWindow(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string windowName,
        uint nodeId);

    [DllImport(CImGui, EntryPoint = "igDockBuilderFinish", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Finish(uint nodeId);
}
