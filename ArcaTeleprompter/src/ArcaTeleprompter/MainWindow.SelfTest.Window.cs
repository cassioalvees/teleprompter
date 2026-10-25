using System.Windows;
using Arca.Core.Engine;

namespace Arca.App;

/// <summary>
/// Autoteste do editor ao mudar o tamanho da janela: maximizar, restaurar e redimensionar — o texto (escala do
/// editor) tem de preencher exatamente a largura visível em cada caso, sem ficar um passo atrás.
/// </summary>
public partial class MainWindow
{
    private async Task WindowTests(Action<string, bool, string> check, Func<int, Task> wait)
    {
        check("— Tamanho da janela —", true, "");
        string Fit(out bool ok)
        {
            var want = EdScroll.ViewportWidth; var got = EdScale.ScaleX * PrompterLayout.Width;
            ok = want > 0 && Math.Abs(got - want) < 1;
            return $"largura visível {want:0}, texto {got:0}";
        }
        var prevState = WindowState; var prevW = Width; var prevH = Height;

        WindowState = WindowState.Normal; Width = 1100; Height = 760; await wait(700);
        var d = Fit(out var ok); check("Janela normal: texto preenche a largura", ok, d);

        WindowState = WindowState.Maximized; await wait(900);
        d = Fit(out ok); check("Maximizar: texto acompanha", ok, d);

        WindowState = WindowState.Normal; await wait(900);
        d = Fit(out ok); check("Restaurar: texto volta ao tamanho certo (não fica enorme)", ok, d);

        Width = 1500; await wait(700);
        d = Fit(out ok); check("Janela mais larga: texto acompanha", ok, d);
        Width = 950; await wait(700);
        d = Fit(out ok); check("Janela mais estreita: texto acompanha", ok, d);

        WindowState = prevState; Width = prevW; Height = prevH; await wait(400);
    }
}
