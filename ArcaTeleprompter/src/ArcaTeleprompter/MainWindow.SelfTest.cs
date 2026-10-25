using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using Arca.Core.Model;

namespace Arca.App;

/// <summary>
/// Autoteste do editor e do transporte (<c>--autoteste=relatorio.txt</c>): exercita os caminhos reais do app —
/// colar, apresentador por linha, desfazer/refazer, controle de velocidade, pausa e linha em branco — e grava um
/// relatório. Usado nos testes de cada versão (com <c>ARCA_DATA_DIR</c> apontando para uma pasta de teste).
/// </summary>
public partial class MainWindow
{
    public async Task SelfTest(string reportPath)
    {
        var r = new StringBuilder();
        var fails = 0;
        void Check(string name, bool ok, string detail = "") { if (!ok) fails++; r.AppendLine($"{(ok ? "OK   " : "FALHA")} {name}{(detail.Length > 0 ? " — " + detail : "")}"); }
        async Task Idle() { await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle); }
        async Task Wait(int ms) { await Task.Delay(ms); await Idle(); }
        // A área de transferência pode estar ocupada por outro programa: tenta algumas vezes.
        async Task SetClip(string t) { for (var i = 0; ; i++) { try { Clipboard.SetDataObject(new DataObject(DataFormats.UnicodeText, t), false); return; } catch when (i < 20) { await Task.Delay(50); } } }

        try
        {
            Pause();
            // Boas-vindas (primeira abertura) e outros diálogos atrapalham o foco do editor: fecha antes.
            while (DialogLayer.Visibility == Visibility.Visible) { CloseDialog(); await Wait(200); }
            if (Vm.Presenters.Count == 0) Vm.AddPresenterVM("Teste", "#3B82F6");

            // 1) Colar várias linhas: cada linha vira um parágrafo e o cursor fica no fim do texto colado.
            LoadEditor(new ScriptDocument { Blocks = { ScriptBlock.Paragraph("") } });
            await Wait(200);
            Editor.Focus();
            Editor.CaretPosition = Editor.Document.ContentStart;
            var oldClip = Clipboard.ContainsText() ? Clipboard.GetText() : null;   // devolve a área de transferência no fim
            await SetClip("Linha um do texto colado.\r\nLinha dois do texto colado.\r\nLinha três do texto colado.");
            Activate(); Editor.Focus();
            Editor.Paste();
            await Wait(300);
            if (oldClip != null) await SetClip(oldClip);
            _editT.Stop(); CommitEdits();
            await Wait(300);
            var texts = Doc.Blocks.Where(b => !string.IsNullOrWhiteSpace(b.PlainText)).ToList();
            Check("Colar 3 linhas vira 3 parágrafos", texts.Count == 3, $"{texts.Count} parágrafo(s) com texto");
            var caret = CaretNow();
            var lastIdx = Doc.Blocks.FindLastIndex(b => !string.IsNullOrWhiteSpace(b.PlainText));
            var lastLen = lastIdx >= 0 ? Doc.Blocks[lastIdx].PlainText.Length : -1;
            Check("Cursor no fim do texto colado", caret.Block == lastIdx && caret.Offset == lastLen, $"cursor bloco {caret.Block} pos {caret.Offset}; esperado {lastIdx}/{lastLen}");

            // 2) Apresentador numa linha só (cursor na 2ª linha, sem seleção).
            var p2 = Editor.Document.Blocks.OfType<Paragraph>().Where(p => new TextRange(p.ContentStart, p.ContentEnd).Text.Trim().Length > 0).ElementAtOrDefault(1);
            if (p2 != null)
            {
                Editor.CaretPosition = p2.ContentStart.GetPositionAtOffset(3) ?? p2.ContentStart;
                Editor.Selection.Select(Editor.CaretPosition, Editor.CaretPosition);
                SetWho(1);
                await Wait(300);
                var whos = Doc.Blocks.Where(b => !string.IsNullOrWhiteSpace(b.PlainText)).Select(b => b.Who).ToArray();
                Check("Apresentador só na linha do cursor", whos.SequenceEqual(new[] { 0, 1, 0 }), "apresentadores: " + string.Join(",", whos));

                // 3) Desfazer / refazer.
                Undo(); await Wait(300);
                var afterUndo = Doc.Blocks.Where(b => !string.IsNullOrWhiteSpace(b.PlainText)).Select(b => b.Who).ToArray();
                Check("Desfazer tira o apresentador", afterUndo.All(w => w == 0), "apresentadores: " + string.Join(",", afterUndo));
                Redo(); await Wait(300);
                var afterRedo = Doc.Blocks.Where(b => !string.IsNullOrWhiteSpace(b.PlainText)).Select(b => b.Who).ToArray();
                Check("Refazer devolve o apresentador", afterRedo.SequenceEqual(new[] { 0, 1, 0 }), "apresentadores: " + string.Join(",", afterRedo));
            }
            else Check("Apresentador só na linha do cursor", false, "2ª linha não encontrada");

            // Digitação também entra no histórico.
            var before = Doc.Blocks.Sum(b => b.PlainText.Length);
            Editor.CaretPosition = Editor.Document.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
            Editor.CaretPosition.InsertTextInRun(" Extra");
            await Wait(100); _editT.Stop(); CommitEdits(); await Wait(200);
            var typed = Doc.Blocks.Sum(b => b.PlainText.Length);
            Undo(); await Wait(300);
            var undone = Doc.Blocks.Sum(b => b.PlainText.Length);
            Check("Desfazer volta a digitação", typed == before + 6 && undone == before, $"{before} → {typed} → {undone} caracteres");

            // 4) Linha em branco: não é linha de leitura; ir até ela leva à próxima linha de texto.
            LoadEditor(new ScriptDocument { Blocks = { ScriptBlock.Paragraph(string.Join(" ", Enumerable.Repeat("Primeiro parágrafo com texto suficiente.", 6))), ScriptBlock.Paragraph(""), ScriptBlock.Paragraph(string.Join(" ", Enumerable.Repeat("Segundo parágrafo depois da linha em branco.", 6))) } });
            await Wait(800);
            Check("Linha em branco não é linha de leitura", !IsReadable(1) && IsReadable(0) && IsReadable(2));
            var blankTop = _measure.TopOf(1); var nextTop = _measure.TopOf(2);
            if (!double.IsNaN(blankTop) && !double.IsNaN(nextTop))
            {
                GoTo(blankTop, textLine: true); await Wait(400);
                Check("Seta pula a linha em branco", Math.Abs(Engine.Y - nextTop) < 2, $"Y={Engine.Y:0.0}, branco={blankTop:0.0}, texto seguinte={nextTop:0.0}");
            }
            else Check("Seta pula a linha em branco", false, "medida do layout indisponível");

            // 5) Controle de velocidade (o mesmo valor do slider da barra de baixo) e pausa sem salto.
            LoadEditor(new ScriptDocument { Blocks = Enumerable.Range(1, 40).Select(i => ScriptBlock.Paragraph($"Parágrafo {i}: texto para medir a rolagem do prompter com calma.")).ToList() });
            await Wait(800);
            Vm.App.Countdown = false;
            Vm.SpeedT = 0.2; Play(); await Wait(1500);
            var v1 = Engine.Velocity;
            Vm.SpeedT = 0.6; await Wait(1500);
            var v2 = Engine.Velocity;
            Check("Slider de velocidade muda a rolagem", v2 > v1 * 1.5 && v1 > 0, $"{v1:0.0} → {v2:0.0} u/s");
            Pause();
            var yPause = Engine.Y; await Wait(800);
            var yAfter = Engine.Y;
            Check("Pausa para onde foi pausado", Math.Abs(yAfter - yPause) < 1, $"andou {yAfter - yPause:0.00} u depois da pausa");
            Play(); await Wait(600); var yLive = Engine.Y; Pause();
            Check("Play continua de onde parou", yLive > yAfter && yLive - yAfter < 400, $"{yAfter:0.0} → {yLive:0.0}");

            await KeyTests(Check, Wait);
        }
        catch (Exception e) { fails++; r.AppendLine("ERRO " + e); }

        r.Insert(0, $"Autoteste ARCA {GetType().Assembly.GetName().Version} — {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n{(fails == 0 ? "TUDO OK" : fails + " falha(s)")}\n\n");
        File.WriteAllText(reportPath, r.ToString());
    }
}
