using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Arca.App.Editor;
using Arca.App.Services;
using Arca.Core.Engine;
using Arca.Core.Model;
using Arca.Core.Net;
using Arca.Core.Storage;

namespace Arca.App;

/// <summary>Ligações com NDI, monitor web/iPad, atalhos globais e o estado publicado para as saídas de rede.</summary>
public partial class MainWindow
{
    public void ToggleNdi()
    {
        if (Ndi.Running) Ndi.Stop(); else Ndi.Start();
    }

    public async Task ToggleWeb(bool? on = null)
    {
        var want = on ?? !Web.Running;
        if (want) await Web.Start(); else await Web.Stop();
    }

    /// <summary>HTML para o navegador (sem blocos pulados, com classes de marcador/pausa/apresentador).</summary>
    public string DisplayHtml() => Arca.Core.Model.DisplayHtml.Build(Doc, Vm.S);

    /// <summary>Estado para o monitor web / iPad.</summary>
    public PrompterSnapshot Snapshot()
    {
        var s = Vm.S; var l = _layout;
        var (il, iw, ih) = l.Indicator(s.IndicatorType);
        var y = Engine.Y; var lh = Math.Max(1, l.LineHeightPx);
        var pos = Engine.SecondsAt(y); var total = Engine.SecondsAt(Engine.EndY);
        var next = _cur + 1 < _bmBoxes.Count ? _bmBoxes[_cur + 1].Top : Engine.EndY;
        var left = Engine.SecondsAt(Math.Max(0, next - y));
        var (sv, su) = ScrollEngine.Format(s.LinesPerSecond, (SpeedUnit)Vm.SpeedUnit, s.WordsPerLine);
        return new PrompterSnapshot
        {
            Bg = s.Background, Fg = s.Foreground, FontFamily = FontCatalog.WebName(s.FontKey), FontSize = l.FontSize, LineH = s.LineHeight, ParaGap = s.ParagraphGap,
            PadL = l.PaddingLeft, PadR = l.PaddingRight, Align = s.Align, ReadPct = s.ReadPct, OvType = s.OverlayType, OvOp = s.OverlayOpacity,
            IndType = s.IndicatorType, IndColor = s.IndicatorColor, IndLeft = il, IndW = iw, IndH = ih, ReadingLine = s.ReadingLine,
            Pres = s.Presenters, PresNames = s.PresenterNames, PresOnly = s.PresenterOnly,
            Live = Engine.Live, Playing = Engine.Playing, Count = Engine.Countdown, Dir = Engine.Direction, SpeedT = Math.Round(s.SpeedT, 3), Speed = $"{sv} {su}",
            Block = _cur >= 0 && _cur < Vm.Bookmarks.Count ? Vm.Bookmarks[_cur].Name : "", BlockLeft = TextUtil.FormatMmSs(left), BlockLeftWarn = left < 10,
            IsLastBlock = _bmBoxes.Count > 1 && _cur == _bmBoxes.Count - 1,
            Stamp = TextUtil.FormatMmSs(pos), Remaining = TextUtil.FormatMmSs(total - pos), Clock = DateTime.Now.ToString("HH:mm:ss"),
            WebCtrl = Vm.App.WebControl, SpeedLock = Engine.SpeedLock, Voice = Vm.VoiceCode,
            Bookmarks = Vm.Bookmarks.Select(b => b.Name).ToList(), Cur = _cur, DocName = CurrentDocName()
        };
    }

    /// <summary>Posição para o navegador: y, velocidade, bloco sob a linha de leitura e deslocamento dentro dele.</summary>
    /// <summary>Posição para o navegador: y, velocidade, bloco âncora, deslocamento, fração do bloco e altura do bloco.</summary>
    public (double Y, double V, int Block, double Offset, double Frac, double BlockH) PositionForWeb()
    {
        var y = Engine.Y;
        var (b, f) = _measure.AnchorAt(y);
        var top = _measure.TopOf(b);
        var h = _measure.Map.FirstOrDefault(x => x.Block == b).Height;
        return (y, ShownVelocity, b, double.IsNaN(top) ? 0 : y - top, f, h);
    }

    // ═══ Atalhos globais (opcional): clicker/pedal funcionam com o app em segundo plano ═══
    private HwndSource? _hwnd;
    private readonly Dictionary<int, string> _hotkeys = new();
    private static readonly string[] GlobalActions = { "play", "nextBm", "prevBm", "faster", "slower", "voice" };

    public void RegisterHotkeys()
    {
        UnregisterHotkeys();
        if (!Vm.App.GlobalHotkeys) return;
        var h = new WindowInteropHelper(this).Handle; if (h == IntPtr.Zero) return;
        _hwnd ??= HwndSource.FromHwnd(h);
        _hwnd.AddHook(HotkeyHook);
        var id = 0xA000;
        foreach (var a in GlobalActions)
        {
            if (!Vm.App.Keymap.TryGetValue(a, out var k) || !Enum.TryParse<Key>(k, out var key)) continue;
            var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            if (Native.RegisterHotKey(h, id, 0x4000 /* MOD_NOREPEAT */, vk)) _hotkeys[id] = a;
            else Log.Warn($"Atalho global {k} já está em uso por outro programa");
            id++;
        }
    }

    public void UnregisterHotkeys()
    {
        var h = new WindowInteropHelper(this).Handle;
        foreach (var id in _hotkeys.Keys) Native.UnregisterHotKey(h, id);
        _hotkeys.Clear();
        _hwnd?.RemoveHook(HotkeyHook);
    }

    private IntPtr HotkeyHook(IntPtr hwnd, int msg, IntPtr wp, IntPtr lp, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY && _hotkeys.TryGetValue(wp.ToInt32(), out var a))
        {
            // Com o app em foco, a tecla já chega pelo teclado normal.
            if (!IsActive) DoAction(a);
            handled = true;
        }
        return IntPtr.Zero;
    }
}
