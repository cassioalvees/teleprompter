using System.Windows;
using System.Windows.Documents;
using Arca.Core.Model;
using Arca.Core.Storage;
using Arca.App.Editor;

namespace Arca.App;

/// <summary>
/// Medição do editor (<c>--cmd=medirEdicao</c>): num roteiro longo, cronometra a releitura depois de digitar, a troca de
/// cor e o Enter — e mede quanto o texto abaixo da linha nova se mexe depois da releitura (o "abre e recua").
/// </summary>
public partial class MainWindow
{
    // Rastreio (só durante a medição): o que rodou e quanto levou, para achar a causa de cada quadro lento.
    private List<(double At, string What, double Ms)>? _trace;
    private readonly System.Diagnostics.Stopwatch _traceClock = System.Diagnostics.Stopwatch.StartNew();
    private void Trace(string what, Action a)
    {
        if (_trace == null) { a(); return; }
        var t0 = _traceClock.Elapsed.TotalMilliseconds; a(); _trace.Add((t0, what, _traceClock.Elapsed.TotalMilliseconds - t0));
    }

    /// <summary>
    /// <c>--cmd=espacos</c>: roteiro com linha em branco entre os parágrafos (como os do usuário). Mede, em linhas, o espaço
    /// entre o fim de um parágrafo e o começo do seguinte no editor — com o cursor longe e com o cursor na linha em
    /// branco — e o mesmo espaço no prompter.
    /// </summary>
    private async Task MeasureBlankGaps()
    {
        var p = "A chuva forte das últimas vinte e quatro horas deixou cidades do litoral em estado de alerta.";
        var doc = new ScriptDocument();
        for (var i = 0; i < 12; i++) { doc.Blocks.Add(ScriptBlock.Paragraph($"{i + 1}. {p}")); if (i < 10) doc.Blocks.Add(ScriptBlock.Paragraph("")); } // os dois últimos sem linha em branco (um Enter)
        LoadEditor(doc); await Task.Delay(1500);
        var line = _layout.FontSize * Vm.S.LineHeight;
        var paras = Editor.Document.Blocks.OfType<Paragraph>().ToList();
        Paragraph a = paras[6], blank = paras[7], b = paras[8];
        double GapOf(Paragraph x, Paragraph y) { Editor.UpdateLayout(); var end = x.ContentEnd.GetCharacterRect(LogicalDirection.Backward); var st = y.ContentStart.GetCharacterRect(LogicalDirection.Forward); return (st.Top - end.Top) / line; }
        double Gap() => GapOf(a, b);
        var one = GapOf(paras[^2], paras[^1]); var inLine = 0.0;
        { var r1 = a.ContentStart.GetCharacterRect(LogicalDirection.Forward); var r2 = a.ContentEnd.GetCharacterRect(LogicalDirection.Backward); inLine = r2.Top - r1.Top; }
        Editor.CaretPosition = paras[0].ContentStart; CommitEdits(); await Task.Delay(300);
        var far = Gap();
        Editor.Focus(); Editor.CaretPosition = blank.ContentStart; await Task.Delay(400);
        var onBlankNow = Gap();
        CommitEdits(); await Task.Delay(400);
        var onBlankAfter = Gap();
        var caret = blank.ContentStart.GetCharacterRect(LogicalDirection.Forward);
        _measure.UpdateLayout();
        var pr = (_measure.TopOf(8) - _measure.LastLineTopOf(6)) / line;
        Log.Info($"Espaços — um Enter {one:0.00} linha · dois Enters (linha em branco) {far:0.00} · " + $"entre parágrafos (em linhas, do topo da última linha ao topo da primeira): editor com cursor longe {far:0.00} · cursor na linha em branco {onBlankNow:0.00} (depois da releitura {onBlankAfter:0.00}) · prompter {pr:0.00} · altura do cursor {caret.Height / line:0.00} linha");
    }

    private async Task MeasureEditing()
    {
        static async Task Wait(int ms) => await Task.Delay(ms);
        var p = "A chuva forte das últimas vinte e quatro horas deixou cidades do litoral em estado de alerta e moradores orientados a deixar as casas.";
        var doc = new ScriptDocument();
        for (var i = 0; i < 60; i++) { if (i % 10 == 0) doc.Blocks.Add(ScriptBlock.Mark("Bloco " + (i / 10 + 1))); doc.Blocks.Add(ScriptBlock.Paragraph($"{i + 1}. {p}")); }
        LoadEditor(doc); await Wait(1200);
        Log.Info($"Edição — roteiro de {doc.Blocks.Count} blocos, {doc.Blocks.Sum(b => b.WordCount)} palavras");

        Paragraph Para(int n) => Editor.Document.Blocks.OfType<Paragraph>().ElementAt(n);
        string Fmt((double Read, double Look, double Compare, double Outputs, double Total) t) => $"ler {t.Read:0} · aparência {t.Look:0} · comparar {t.Compare:0} · telas {t.Outputs:0} · total {t.Total:0} ms";

        // Digitar: 8 pausas (cada uma dispara uma releitura)
        var totals = new List<double>();
        for (var i = 0; i < 8; i++)
        {
            Editor.CaretPosition = Para(30).ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
            Editor.CaretPosition.InsertTextInRun(" palavra");
            CommitEdits(); totals.Add(LastCommitMs.Total);
            if (i == 7) Log.Info("Edição — digitar (última): " + Fmt(LastCommitMs) + " · telas: " + LastRebuildMs + $" · parágrafos trocados {_measure.LastPatch.Removed}→{_measure.LastPatch.Inserted} (mantidos {_measure.LastPatch.Kept} no começo)");
            await Wait(80);
        }
        Log.Info($"Edição — digitar: releitura média {totals.Average():0} ms (máx {totals.Max():0})");

        // Digitação real: uma letra a cada 60 ms, pausa de 400 ms a cada palavra; as releituras vêm sozinhas (pausa de
        // 250 ms). Mede o maior intervalo entre quadros desenhados — o que se sente como "travadinha".
        var gaps = new List<double>(); var last = 0.0; var clock = _traceClock; var slow = new List<(double From, double To)>();
        _trace = new();
        EventHandler onFrame = (_, _) => { var t = clock.Elapsed.TotalMilliseconds; if (last > 0) { gaps.Add(t - last); if (t - last > 50) slow.Add((last, t)); } last = t; };
        System.Windows.Media.CompositionTarget.Rendering += onFrame;
        Editor.Focus();
        Editor.CaretPosition = Para(40).ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
        foreach (var word in new[] { " o", " texto", " novo", " entra", " aqui", " agora" })
        {
            foreach (var ch in word) { Trace("tecla", () => { Editor.CaretPosition.InsertTextInRun(ch.ToString()); Editor.UpdateLayout(); }); Editor.CaretPosition = Editor.CaretPosition.GetPositionAtOffset(1, LogicalDirection.Forward) ?? Editor.CaretPosition; await Wait(60); }
            await Wait(400);
        }
        await Wait(1200);
        System.Windows.Media.CompositionTarget.Rendering -= onFrame;
        foreach (var (a, b) in slow.Take(12))
        {
            var inside = _trace.Where(e => e.At < b && e.At + e.Ms > a).Select(e => $"{e.What} {e.Ms:0}");
            Log.Info($"Edição — quadro lento {b - a:0} ms: {string.Join(", ", inside.DefaultIfEmpty("(nada rastreado: desenho/diagramação do WPF)"))}");
        }
        _trace = null;
        gaps.Sort();
        Log.Info($"Edição — digitação real: {gaps.Count} quadros · mediana {gaps[gaps.Count / 2]:0} ms · p95 {gaps[(int)(gaps.Count * 0.95)]:0} ms · maior intervalo {gaps[^1]:0} ms · acima de 50 ms: {gaps.Count(g => g > 50)}");

        // Trocar a cor de uma palavra (botão da barra)
        var para = Para(20); var start = para.ContentStart.GetPositionAtOffset(4); var end = start?.GetPositionAtOffset(10);
        if (start != null && end != null)
        {
            Editor.Selection.Select(start, end);
            var sw = System.Diagnostics.Stopwatch.StartNew(); SetColor("#FFD400"); var ms = sw.Elapsed.TotalMilliseconds;
            Log.Info($"Edição — trocar a cor: {ms:0} ms até a cor aparecer (" + Fmt(LastCommitMs) + ")");
        }
        await Wait(300);

        // Enter no fim de um parágrafo: quanto o texto de baixo se mexe depois da releitura
        double TopOf(Block b) { var r = b.ContentStart.GetCharacterRect(LogicalDirection.Forward); return r.IsEmpty ? double.NaN : r.Top; }
        var target = Para(25); var below = target.NextBlock?.NextBlock ?? target.NextBlock;
        Editor.CaretPosition = target.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
        EditingCommands.EnterParagraphBreak.Execute(null, Editor);
        Editor.UpdateLayout(); await Wait(30);
        var after = target.NextBlock; var probe = after?.NextBlock;
        var before = probe != null ? TopOf(probe) : double.NaN;
        CommitEdits(); Editor.UpdateLayout(); await Wait(30);
        var moved = probe != null ? TopOf(probe) - before : double.NaN;
        Log.Info($"Edição — Enter: texto de baixo se mexeu {moved:0.0} px depois da releitura (linha nova vazia; 0 = sem pulo) · " + Fmt(LastCommitMs));
        Editor.CaretPosition.InsertTextInRun("Texto novo");
        Editor.UpdateLayout(); var b2 = probe != null ? TopOf(probe) : double.NaN;
        CommitEdits(); Editor.UpdateLayout(); await Wait(30);
        Log.Info($"Edição — digitar na linha nova: texto de baixo se mexeu {(probe != null ? TopOf(probe) - b2 : double.NaN):0.0} px depois da releitura");
        _ = below;

        // Como se usa de verdade: Enter duas vezes (pula uma linha) e começa a digitar; depois clica numa linha em branco e
        // digita. Acompanha quadro a quadro onde fica o parágrafo de baixo: só pode descer; subir é o "abre e recua".
        async Task Track(string name, Block probeB, Func<Task> act)
        {
            var tops = new List<(double T, double Top)>(); var t0 = _traceClock.Elapsed.TotalMilliseconds;
            EventHandler f = (_, _) => { var y = TopOf(probeB); if (tops.Count == 0 || Math.Abs(tops[^1].Top - y) > 0.5) tops.Add((_traceClock.Elapsed.TotalMilliseconds - t0, y)); };
            System.Windows.Media.CompositionTarget.Rendering += f;
            await act(); await Wait(900);
            System.Windows.Media.CompositionTarget.Rendering -= f;
            double down = 0, up = 0;
            for (var i = 1; i < tops.Count; i++) { var d = tops[i].Top - tops[i - 1].Top; if (d > 0) down += d; else up -= d; }
            Log.Info($"Edição — {name}: desceu {down:0.0} px, recuou {up:0.0} px (0 = sem recuo) · " + string.Join(" → ", tops.Select(x => $"{x.Top:0}@{x.T:0}ms")));
        }
        async Task TypeText(string s) { foreach (var ch in s) { Editor.CaretPosition.InsertTextInRun(ch.ToString()); Editor.CaretPosition = Editor.CaretPosition.GetPositionAtOffset(1, LogicalDirection.Forward) ?? Editor.CaretPosition; await Wait(60); } }
        var host = Para(12); var probe2 = host.NextBlock!;
        Editor.CaretPosition = host.ContentEnd.GetInsertionPosition(LogicalDirection.Backward); await Wait(400);
        await Track("Enter duas vezes e digitar", probe2, async () =>
        {
            EditingCommands.EnterParagraphBreak.Execute(null, Editor); await Wait(150);
            EditingCommands.EnterParagraphBreak.Execute(null, Editor); await Wait(150);
            await TypeText("Texto novo");
        });
        var blankLine = host.NextBlock as Paragraph;
        if (blankLine != null)
        {
            Editor.CaretPosition = blankLine.ContentStart.GetInsertionPosition(LogicalDirection.Forward); await Wait(400);
            await Track("digitar numa linha em branco", probe2, () => TypeText("Mais"));
        }
        // Parágrafo com nome de apresentador: Enter no fim dele.
        if (Vm.S.Presenters.Count > 0)
        {
            var pres = Para(14); pres.Tag = new BlockTag { Who = 1 }; CommitEdits(); await Wait(400);
            var probe3 = pres.NextBlock!;
            Editor.CaretPosition = pres.ContentEnd.GetInsertionPosition(LogicalDirection.Backward); await Wait(200);
            await Track("Enter no fim de parágrafo com apresentador", probe3, async () => { EditingCommands.EnterParagraphBreak.Execute(null, Editor); await Wait(150); await TypeText("Novo"); });
        }
    }
}
