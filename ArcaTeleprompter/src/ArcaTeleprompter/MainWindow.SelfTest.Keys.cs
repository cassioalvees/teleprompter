using System.Windows;
using System.Windows.Input;
using Arca.Core.Model;

namespace Arca.App;

/// <summary>
/// Autoteste dos atalhos de teclado e dos botões da barra de controle: cada tecla passa pelo mesmo caminho do
/// teclado de verdade (<see cref="HandleKey"/>) e o efeito é conferido no motor de rolagem.
/// </summary>
public partial class MainWindow
{
    private async Task KeyTests(Action<string, bool, string> check, Func<int, Task> wait)
    {
        void Check(string n, bool ok, string d = "") => check(n, ok, d);
        bool Press(Key k, ModifierKeys m = ModifierKeys.None, bool fromOutput = true)
        {
            var e = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 0, k) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
            HandleKey(e, fromOutput, m);
            return e.Handled;
        }

        // Roteiro com 3 marcadores e vários parágrafos.
        var doc = new ScriptDocument();
        for (var b = 1; b <= 3; b++)
        {
            doc.Blocks.Add(ScriptBlock.Mark($"Bloco {b}"));
            for (var i = 1; i <= 8; i++) doc.Blocks.Add(ScriptBlock.Paragraph($"Bloco {b}, parágrafo {i}: texto de teste para conferir os atalhos do teclado com calma."));
        }
        LoadEditor(doc);
        await wait(900);
        Vm.App.Countdown = false;
        // Com um diálogo aberto (ex.: boas-vindas) o app ignora os atalhos de propósito: fecha antes.
        while (DialogLayer.Visibility == Visibility.Visible) { CloseDialog(); await wait(200); }
        Keyboard.ClearFocus();
        var lineH = _layout.LineHeightPx;
        r("— Atalhos de teclado —");

        // Espaço: play / pausa
        GoTo(0, textLine: true); await wait(100);
        Press(Key.Space); await wait(250); var live1 = Engine.Live;
        Press(Key.Space); await wait(250); var live2 = Engine.Live;
        Check("Espaço: play e pausa", live1 && !live2);

        // ↓ / ↑ parado: próxima / linha anterior
        GoTo(0, textLine: true); await wait(150);
        var y0 = Engine.Y; Press(Key.Down); await wait(450); var y1 = Engine.Y; Press(Key.Up); await wait(450); var y2 = Engine.Y;
        Check("↓ / ↑ parado: linha seguinte e anterior", y1 > y0 + lineH * 0.5 && Math.Abs(y2 - y0) < 2, $"{y0:0} → {y1:0} → {y2:0}");

        // ↓ / ↑, W / S rodando: velocidade
        Vm.SpeedT = 0.3; Play(); await wait(200);
        var s0 = Vm.SpeedT; Press(Key.Down); await wait(100); var s1 = Vm.SpeedT; Press(Key.Up); await wait(100); var s2 = Vm.SpeedT;
        Check("↓ / ↑ rodando: mais rápido e mais devagar", s1 > s0 && Math.Abs(s2 - s0) < 0.001, $"{s0:0.00} → {s1:0.00} → {s2:0.00}");
        Press(Key.W); await wait(100); var w1 = Vm.SpeedT; Press(Key.S); await wait(100); var w2 = Vm.SpeedT;
        Check("W / S: mais rápido e mais devagar", w1 > s2 && Math.Abs(w2 - s2) < 0.001, $"{s2:0.00} → {w1:0.00} → {w2:0.00}");

        // Frear até o fim não vira ré; R inverte
        for (var i = 0; i < 40; i++) Press(Key.S);
        await wait(600);
        Check("Frear até o mínimo não vira ré", Engine.Direction > 0 && Vm.SpeedT <= 0.001, $"velocidade {Vm.SpeedT:0.00}, direção {Engine.Direction}");
        Vm.SpeedT = 0.3; await wait(100);
        Press(Key.R); await wait(100); var d1 = Engine.Direction; var rv1 = Vm.Reverse;
        Press(Key.R); await wait(100); var d2 = Engine.Direction;
        Check("R: inverte e desinverte a direção", d1 < 0 && rv1 && d2 > 0, $"direção {d1} → {d2}");

        // 0: parar
        Press(Key.D0); await wait(200);
        Check("0: para a rolagem", !Engine.Live);

        // Teclas 1–9: velocidades prontas
        Press(Key.D9); await wait(100); var p9 = Vm.SpeedT; Press(Key.D1); await wait(100); var p1 = Vm.SpeedT;
        Check("1–9: velocidades prontas", Math.Abs(p9 - 1) < 0.001 && p1 < 0.001, $"9 → {p9:0.00}, 1 → {p1:0.00}");
        Press(Key.NumPad5); await wait(100);
        Check("Teclado numérico 1–9", Math.Abs(Vm.SpeedT - 0.5) < 0.001, $"5 → {Vm.SpeedT:0.00}");
        Vm.SpeedT = 0.3;

        // End / Home / Backspace
        Press(Key.End); await wait(200); var yEnd = Engine.Y;
        Check("End: vai ao fim", Math.Abs(yEnd - Engine.EndY) < 1, $"{yEnd:0} de {Engine.EndY:0}");
        Press(Key.Home); await wait(200); var yHome = Engine.Y;
        Check("Home: volta à 1ª linha de texto", yHome < yEnd && IsTextLine(yHome), $"Y={yHome:0}");
        Press(Key.End); await wait(150); Press(Key.Back); await wait(200);
        Check("Backspace: volta ao início", Math.Abs(Engine.Y - yHome) < 1, $"Y={Engine.Y:0}");

        // Page Down / Page Up: marcadores
        Press(Key.Home); await wait(150);
        var c0 = _cur; Press(Key.PageDown); await wait(300); var c1 = _cur; var yb = Engine.Y; Press(Key.PageUp); await wait(300); var c2 = _cur;
        Check("Page Down / Page Up: próximo e anterior marcador", c1 == c0 + 1 && c2 == c0 && IsTextLine(yb), $"marcador {c0} → {c1} → {c2}");

        // ] / [: parágrafos
        Press(Key.Home); await wait(150);
        var a0 = _measure.AnchorAt(Engine.Y + 1).Block; Press(Key.OemCloseBrackets); await wait(300); var a1 = _measure.AnchorAt(Engine.Y + 1).Block;
        Press(Key.OemOpenBrackets); await wait(300); var a2 = _measure.AnchorAt(Engine.Y + 1).Block;
        Check("] / [: próximo e anterior parágrafo", a1 == a0 + 1 && a2 == a0, $"bloco {a0} → {a1} → {a2}");

        // M: espelhar
        var tv = Outputs.PreviewTarget; bool Mir() => tv?.FlipH ?? _talentFlipH;
        var m0 = Mir(); Press(Key.M); await wait(100); var m1 = Mir(); Press(Key.M); await wait(100); var m2 = Mir();
        Check("M: espelha e desespelha", m1 != m0 && m2 == m0);

        // F5: abre e fecha a janela do talento; Esc também fecha
        Press(Key.F5); await wait(800); var tOpen = TalentLayer.Visibility == Visibility.Visible;
        Press(Key.F5); await wait(500); var tClosed = TalentLayer.Visibility != Visibility.Visible;
        Check("F5: abre e fecha a janela do talento", tOpen && tClosed);
        Press(Key.F5); await wait(800); Press(Key.Escape, fromOutput: false); await wait(400);
        Check("Esc: fecha a janela do talento", TalentLayer.Visibility != Visibility.Visible);

        // E (com o talento aberto): fecha o talento e leva o cursor do editor ao trecho que está na seta
        Press(Key.Home); await wait(150); Press(Key.OemCloseBrackets); await wait(300);
        var blk = CurrentBlock();
        Press(Key.F5); await wait(800);
        Press(Key.E, fromOutput: false); await wait(500);
        var edBlk = CaretNow().Block;
        Check("E: edita o trecho atual no editor", TalentLayer.Visibility != Visibility.Visible && !Vm.Locked && edBlk == blk, $"bloco na seta {blk}, cursor no bloco {edBlk}");
        Keyboard.ClearFocus();

        // F11: tela cheia
        Press(Key.F11); await wait(400); var fs1 = _fullscreen; Press(Key.F11); await wait(400); var fs2 = _fullscreen;
        Check("F11: tela cheia e volta", fs1 && !fs2);

        // Ctrl+Enter, Ctrl+1/2, Ctrl+E, Ctrl+F, Ctrl+,
        Press(Key.Enter, ModifierKeys.Control); await wait(250); var ce1 = Engine.Live; Press(Key.Enter, ModifierKeys.Control); await wait(250);
        Check("Ctrl+Enter: play e pausa", ce1 && !Engine.Live);
        Press(Key.D2, ModifierKeys.Control); await wait(300); var cb2 = _cur; Press(Key.D1, ModifierKeys.Control); await wait(300); var cb1 = _cur;
        Check("Ctrl+1…9: vai ao marcador", cb2 == 1 && cb1 == 0, $"Ctrl+2 → {cb2}, Ctrl+1 → {cb1}");
        var l0 = Vm.Locked; Press(Key.E, ModifierKeys.Control); await wait(150); var l1 = Vm.Locked; Press(Key.E, ModifierKeys.Control); await wait(150);
        Check("Ctrl+E: trava e destrava o editor", l1 != l0 && Vm.Locked == l0);
        Press(Key.OemComma, ModifierKeys.Control); await wait(400); var pref = DialogLayer.Visibility == Visibility.Visible;
        Press(Key.Escape); await wait(200);
        Check("Ctrl+, abre Preferências; Esc fecha", pref && DialogLayer.Visibility != Visibility.Visible);

        // Teclas não roubam a digitação: no editor (destravado), R e Espaço são texto.
        if (Vm.Locked) ToggleLock();
        Editor.Focus(); await wait(100);
        var dr = Engine.Direction; var hr = Press(Key.R, fromOutput: false);
        Check("No editor, R é texto (não inverte)", !hr && Engine.Direction == dr);
        Keyboard.ClearFocus();

        // V: voz (só confere que a tecla está ligada à ação; não abre o microfone no teste)
        Check("V: ligado a \"voz\" no mapa de teclas", Vm.App.Keymap.TryGetValue("voice", out var vk) && vk == "V", vk ?? "—");

        // Botões da barra de controle (mesmos em todas as telas): cada comando tem efeito.
        r("— Barra de controle —");
        GoTo(0, textLine: true); await wait(150);
        async Task<double> Cmd(string c) { RemoteCommand(c, null); await wait(350); return Engine.Y; }
        var b0 = Engine.Y;
        Check("Botão próxima linha", await Cmd("lineDown") > b0);
        var b1 = Engine.Y; Check("Botão linha anterior", await Cmd("lineUp") < b1);
        var b2 = Engine.Y; Check("Botão próximo parágrafo", await Cmd("nextPara") > b2);
        var b3 = Engine.Y; Check("Botão parágrafo anterior", await Cmd("prevPara") < b3);
        await Cmd("togglePlay"); var bl = Engine.Live; await Cmd("togglePlay");
        Check("(diagnóstico) estado", true, $"Y={Engine.Y:0}/{Engine.EndY:0} dir={Engine.Direction} live={Engine.Live} diálogo={DialogLayer.Visibility}");
        Check("Botão play / pausa", bl && !Engine.Live);
        var sp = Vm.SpeedT; RemoteCommand("faster", null); var sp1 = Vm.SpeedT; RemoteCommand("slower", null);
        Check("Botões + e −", sp1 > sp && Math.Abs(Vm.SpeedT - sp) < 0.001, $"{sp:0.00} → {sp1:0.00} → {Vm.SpeedT:0.00}");
        RemoteCommand("reverse", null); var rr = Engine.Direction; RemoteCommand("reverse", null);
        Check("Botão ré", rr < 0 && Engine.Direction > 0);
        Press(Key.End); await wait(150);
        Check("Botão início", await Cmd("rewind") < Engine.EndY && IsTextLine(Engine.Y));

        await SmoothTests(Check, wait);
        await WindowTests(Check, wait);

        void r(string t) => check(t, true, "");
    }
}
