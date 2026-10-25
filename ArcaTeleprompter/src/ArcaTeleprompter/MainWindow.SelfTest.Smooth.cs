using System.Windows;
using System.Windows.Input;

namespace Arca.App;

/// <summary>
/// Autoteste da suavidade: rola com a janela do talento e uma janela de prompter abertas, acelera e freia pelas teclas
/// (W/S) e registra cada quadro desenhado — saltos de velocidade entre quadros, aceleração e quadros atrasados.
/// </summary>
public partial class MainWindow
{
    private async Task SmoothTests(Action<string, bool, string> check, Func<int, Task> wait)
    {
        check("— Suavidade ao acelerar e frear —", true, "");
        bool Press(Key k)
        {
            var e = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 0, k) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
            HandleKey(e, true, ModifierKeys.None);
            return e.Handled;
        }

        if (TalentLayer.Visibility != Visibility.Visible) ToggleTalent();
        Outputs.AddWindow();
        await wait(1200);

        var frames = new List<(double T, double Y)>();
        FrameProbe = (t, y) => frames.Add((t, y));
        GoTo(0, textLine: true); Vm.SpeedT = 0.3; Play();
        await wait(1000);
        for (var i = 0; i < 4; i++) Press(Key.W);   // acelera 4 passos de uma vez
        await wait(1500);
        for (var i = 0; i < 4; i++) Press(Key.S);   // freia 4 passos
        await wait(1500);
        for (var i = 0; i < 8; i++) Press(Key.W);   // acelera forte (velocidade alta)
        await wait(1200);
        for (var i = 0; i < 8; i++) Press(Key.S);   // freia forte a partir da velocidade alta
        await wait(1500);
        FrameProbe = null;
        Pause();

        // Velocidade em cada quadro (o motor anda exatamente o dt do quadro) e saltos entre quadros.
        var dts = new List<double>(); var vs = new List<double>(); var acc = new List<double>();
        for (var i = 1; i < frames.Count; i++)
        {
            var dt = frames[i].T - frames[i - 1].T; if (dt <= 0) continue;
            dts.Add(dt); vs.Add((frames[i].Y - frames[i - 1].Y) / dt);
        }
        for (var i = 1; i < vs.Count; i++) acc.Add((vs[i] - vs[i - 1]) / dts[i]);
        var med = dts.OrderBy(x => x).ElementAt(dts.Count / 2);
        var late = dts.Count(d => d > 0.05);
        var vmax = vs.Max();
        var jump = vs.Zip(vs.Skip(1), (a, b) => Math.Abs(b - a)).Max();
        var amax = acc.Select(Math.Abs).Max();
        // Inversões de aceleração fora das mudanças de velocidade (tranco = acelera/freia/acelera em sequência).
        var flips = 0;
        for (var i = 2; i < acc.Count; i++) if (Math.Abs(acc[i]) > 50 && Math.Abs(acc[i - 1]) > 50 && Math.Sign(acc[i]) != Math.Sign(acc[i - 1])) flips++;
        var neg = vs.Count(v => v < -0.5);

        check("Quadros: sem atraso visível", late <= 2, $"{frames.Count} quadros em {frames[^1].T - frames[0].T:0.0} s, intervalo mediano {med * 1000:0.0} ms, {late} acima de 50 ms (máx {dts.Max() * 1000:0} ms)");
        check("Mudança de velocidade sem degrau", jump <= Math.Max(30, vmax * 0.08), $"maior salto entre quadros {jump:0.0} u/s (velocidade máx {vmax:0} u/s)");
        check("Aceleração constante, sem tranco", flips == 0, $"aceleração máx {amax:0} u/s², {flips} inversão(ões)");
        check("Frear forte não vira ré", neg == 0, $"{neg} quadro(s) andando para trás");

        if (TalentLayer.Visibility == Visibility.Visible) ToggleTalent();
        foreach (var d in Vm.Displays.Where(d => d.Windowed).ToList()) d.On = false;
        await wait(300);
    }
}
