using System.Reflection;
using System.Runtime.InteropServices;

namespace Arca.Ndi;

/// <summary>
/// Ligação (P/Invoke) com o NDI Runtime oficial (Processing.NDI.Lib.x64.dll).
/// A DLL não é redistribuída pelo app: ela vem do "NDI Runtime" ou das "NDI Tools", que gravam o caminho
/// na variável de ambiente NDI_RUNTIME_DIR_V6 (ou V5). Estruturas conforme Processing.NDI.structs.h (SDK 5/6).
/// </summary>
internal static class NdiNative
{
    public const string Lib = "Processing.NDI.Lib.x64.dll";

    static NdiNative()
    {
        NativeLibrary.SetDllImportResolver(typeof(NdiNative).Assembly, Resolve);
    }

    /// <summary>Caminho da DLL encontrada (ou null).</summary>
    public static string? FoundPath { get; private set; }

    public static IEnumerable<string> Candidates()
    {
        foreach (var v in new[] { "NDI_RUNTIME_DIR_V6", "NDI_RUNTIME_DIR_V5", "NDI_RUNTIME_DIR_V4" })
            if (Environment.GetEnvironmentVariable(v) is { Length: > 0 } dir) yield return Path.Combine(dir, Lib);
        yield return Path.Combine(AppContext.BaseDirectory, Lib);
        // SDK instalado (NDI_SDK_DIRBind): serve quando o runtime não está.
        if (Environment.GetEnvironmentVariable("NDI_SDK_DIR") is { Length: > 0 } sdk) yield return Path.Combine(sdk, "Bin", "x64", Lib);
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NDI", "NDI 6 Runtime", "v6", Lib);
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NDI", "NDI 6 Tools", "Runtime", Lib);
    }

    private static IntPtr Resolve(string name, Assembly asm, DllImportSearchPath? path)
    {
        if (name != Lib) return IntPtr.Zero;
        foreach (var c in Candidates())
            if (File.Exists(c) && NativeLibrary.TryLoad(c, out var h)) { FoundPath = c; return h; }
        return IntPtr.Zero;
    }

    public static bool TryLoad()
    {
        try { return NDIlib_initialize(); }
        catch (DllNotFoundException) { return false; }
        catch (EntryPointNotFoundException) { return false; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SendCreate
    {
        public IntPtr p_ndi_name;
        public IntPtr p_groups;
        [MarshalAs(UnmanagedType.U1)] public bool clock_video;
        [MarshalAs(UnmanagedType.U1)] public bool clock_audio;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VideoFrameV2
    {
        public int xres, yres;
        public uint FourCC;
        public int frame_rate_N, frame_rate_D;
        public float picture_aspect_ratio;
        public int frame_format_type;
        public long timecode;
        public IntPtr p_data;
        public int line_stride_in_bytes;
        public IntPtr p_metadata;
        public long timestamp;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Tally
    {
        [MarshalAs(UnmanagedType.U1)] public bool on_program;
        [MarshalAs(UnmanagedType.U1)] public bool on_preview;
    }

    public const uint FourCC_BGRA = 'B' | ('G' << 8) | ('R' << 16) | ('A' << 24);
    public const uint FourCC_BGRX = 'B' | ('G' << 8) | ('R' << 16) | ('X' << 24);
    public const int FrameProgressive = 1;
    public const long TimecodeSynthesize = long.MaxValue;

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.U1)] public static extern bool NDIlib_initialize();
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern void NDIlib_destroy();
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr NDIlib_version();

    /// <summary>NDIlib_metadata_frame_t (Processing.NDI.structs.h).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MetadataFrame { public int length; public long timecode; public IntPtr p_data; }
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern void NDIlib_send_add_connection_metadata(IntPtr instance, ref MetadataFrame metadata);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr NDIlib_send_create(ref SendCreate settings);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern void NDIlib_send_destroy(IntPtr instance);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern void NDIlib_send_send_video_v2(IntPtr instance, ref VideoFrameV2 frame);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern void NDIlib_send_send_video_async_v2(IntPtr instance, IntPtr frame);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern int NDIlib_send_get_no_connections(IntPtr instance, uint timeoutMs);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.U1)] public static extern bool NDIlib_send_get_tally(IntPtr instance, ref Tally tally, uint timeoutMs);
}
