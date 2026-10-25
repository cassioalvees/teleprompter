using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Arca.App.Services;

/// <summary>Chamadas Win32 usadas pelo app (seletor de cor nativo, monitores, atalhos globais).</summary>
public static class Native
{
    // ——— Seletor de cor do Windows (comdlg32) ———
    [StructLayout(LayoutKind.Sequential)]
    private struct CHOOSECOLOR
    {
        public int lStructSize; public IntPtr hwndOwner; public IntPtr hInstance; public int rgbResult;
        public IntPtr lpCustColors; public int Flags; public IntPtr lCustData; public IntPtr lpfnHook; public IntPtr lpTemplateName;
    }
    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode)] private static extern bool ChooseColorW(ref CHOOSECOLOR cc);
    private static readonly int[] Custom = new int[16];

    /// <summary>Abre o seletor de cor do Windows. Retorna "#rrggbb" ou null se cancelado.</summary>
    public static string? PickColor(Window owner, string current)
    {
        var c = Colors.White;
        try { c = (Color)ColorConverter.ConvertFromString(current); } catch { /* */ }
        var h = GCHandle.Alloc(Custom, GCHandleType.Pinned);
        try
        {
            var cc = new CHOOSECOLOR
            {
                lStructSize = Marshal.SizeOf<CHOOSECOLOR>(), hwndOwner = new WindowInteropHelper(owner).Handle,
                rgbResult = c.R | c.G << 8 | c.B << 16, lpCustColors = h.AddrOfPinnedObject(), Flags = 0x1 | 0x2 /* RGBINIT | FULLOPEN */
            };
            if (!ChooseColorW(ref cc)) return null;
            return $"#{cc.rgbResult & 0xFF:x2}{cc.rgbResult >> 8 & 0xFF:x2}{cc.rgbResult >> 16 & 0xFF:x2}";
        }
        finally { h.Free(); }
    }

    // ——— Monitores ———
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX { public int cbSize; public RECT rcMonitor; public RECT rcWork; public int dwFlags; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string szDevice; }
    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc cb, IntPtr data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX info);

    public sealed record MonitorInfo(string Device, Int32Rect Bounds, bool Primary, string FriendlyName)
    {
        public override string ToString() => FriendlyName;
    }

    /// <summary>Monitores conectados, com nome comercial (ex.: "LG ULTRAGEAR") quando o Windows informa.</summary>
    public static List<MonitorInfo> Monitors()
    {
        var names = FriendlyNames();
        var list = new List<MonitorInfo>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hm, IntPtr _, ref RECT _, IntPtr _) =>
        {
            var mi = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
            if (GetMonitorInfo(hm, ref mi))
            {
                var r = mi.rcMonitor;
                names.TryGetValue(mi.szDevice, out var fn);
                list.Add(new MonitorInfo(mi.szDevice, new Int32Rect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top), (mi.dwFlags & 1) != 0, fn ?? ""));
            }
            return true;
        }, IntPtr.Zero);
        return list;
    }

    // DisplayConfig: nome amigável do monitor por dispositivo (\\.\DISPLAY1 → "LG 24MK430").
    [StructLayout(LayoutKind.Sequential)] private struct LUID { public uint Low; public int High; }
    [StructLayout(LayoutKind.Sequential)] private struct DISPLAYCONFIG_PATH_SOURCE_INFO { public LUID adapterId; public uint id; public uint modeInfoIdx; public uint statusFlags; }
    [StructLayout(LayoutKind.Sequential)] private struct DISPLAYCONFIG_RATIONAL { public uint Numerator, Denominator; }
    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_TARGET_INFO
    {
        public LUID adapterId; public uint id; public uint modeInfoIdx; public uint outputTechnology; public uint rotation; public uint scaling;
        public DISPLAYCONFIG_RATIONAL refreshRate; public uint scanLineOrdering; public int targetAvailable; public uint statusFlags;
    }
    [StructLayout(LayoutKind.Sequential)] private struct DISPLAYCONFIG_PATH_INFO { public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo; public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo; public uint flags; }
    [StructLayout(LayoutKind.Sequential, Size = 64)] private struct DISPLAYCONFIG_MODE_INFO { public uint infoType; public uint id; public LUID adapterId; }
    [StructLayout(LayoutKind.Sequential)] private struct DISPLAYCONFIG_DEVICE_INFO_HEADER { public uint type; public uint size; public LUID adapterId; public uint id; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_TARGET_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header; public uint flags; public uint outputTechnology; public ushort edidManufactureId; public ushort edidProductCodeId; public uint connectorInstance;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string monitorFriendlyDeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string monitorDevicePath;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_SOURCE_DEVICE_NAME { public DISPLAYCONFIG_DEVICE_INFO_HEADER header; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string viewGdiDeviceName; }
    [DllImport("user32.dll")] private static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPaths, out uint numModes);
    [DllImport("user32.dll")] private static extern int QueryDisplayConfig(uint flags, ref uint numPaths, [Out] DISPLAYCONFIG_PATH_INFO[] paths, ref uint numModes, [Out] DISPLAYCONFIG_MODE_INFO[] modes, IntPtr topology);
    [DllImport("user32.dll")] private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME req);
    [DllImport("user32.dll")] private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME req);

    private static Dictionary<string, string> FriendlyNames()
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            const uint QDC_ONLY_ACTIVE_PATHS = 2;
            if (GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out var np, out var nm) != 0) return d;
            var paths = new DISPLAYCONFIG_PATH_INFO[np]; var modes = new DISPLAYCONFIG_MODE_INFO[nm];
            if (QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref np, paths, ref nm, modes, IntPtr.Zero) != 0) return d;
            foreach (var p in paths.Take((int)np))
            {
                var tn = new DISPLAYCONFIG_TARGET_DEVICE_NAME { header = { type = 2, size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>(), adapterId = p.targetInfo.adapterId, id = p.targetInfo.id } };
                var sn = new DISPLAYCONFIG_SOURCE_DEVICE_NAME { header = { type = 1, size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>(), adapterId = p.sourceInfo.adapterId, id = p.sourceInfo.id } };
                if (DisplayConfigGetDeviceInfo(ref tn) == 0 && DisplayConfigGetDeviceInfo(ref sn) == 0 && !string.IsNullOrWhiteSpace(tn.monitorFriendlyDeviceName))
                    d[sn.viewGdiDeviceName] = tn.monitorFriendlyDeviceName;
            }
        }
        catch { /* versões antigas do Windows */ }
        return d;
    }

    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    /// <summary>Retângulo (pixels) do monitor onde a janela está.</summary>
    public static Int32Rect MonitorFor(IntPtr hwnd)
    {
        var hm = MonitorFromWindow(hwnd, 2 /* NEAREST */);
        var mi = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
        if (!GetMonitorInfo(hm, ref mi)) return new Int32Rect(0, 0, 1920, 1080);
        var r = mi.rcMonitor; return new Int32Rect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
    }

    // ——— Janela em coordenadas físicas (monitores com DPI diferente) ———
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    public static void PlaceWindow(Window w, Int32Rect r)
    {
        var h = new WindowInteropHelper(w).Handle;
        if (h != IntPtr.Zero) SetWindowPos(h, IntPtr.Zero, r.X, r.Y, r.Width, r.Height, 0x0004 /* NOZORDER */ | 0x0010 /* NOACTIVATE */);
    }

    // ——— Atalhos globais (clicker/pedal com o app em segundo plano) ———
    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint mods, uint vk);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // ——— Manter a tela ligada durante o prompt ———
    [DllImport("kernel32.dll")] private static extern uint SetThreadExecutionState(uint flags);
    public static void KeepAwake(bool on) => SetThreadExecutionState(on ? 0x80000000u | 0x00000002u | 0x00000001u : 0x80000000u);
}
