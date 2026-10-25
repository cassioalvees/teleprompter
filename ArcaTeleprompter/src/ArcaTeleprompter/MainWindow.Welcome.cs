using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Arca.App.Controls;

namespace Arca.App;

/// <summary>Por que a tela de boas-vindas está sendo mostrada.</summary>
public enum WelcomeKind
{
    /// <summary>Primeira vez que o app abre neste usuário (ou instalação nova).</summary>
    First,
    /// <summary>Atualização: mostra as novidades da versão.</summary>
    Upgrade,
    /// <summary>Aberta pelo menu Ajuda.</summary>
    Manual
}

/// <summary>Tela de boas-vindas (primeira execução, atualização e Ajuda › Boas-vindas).</summary>
public partial class MainWindow
{
    public static string AppVersion => typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "";

    public void ShowWelcome(WelcomeKind kind, string? fromVersion = null)
    {
        var sp = new StackPanel();

        // Cabeçalho
        var head = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 18) };
        head.Children.Add(new Image { Source = new BitmapImage(new Uri("pack://application:,,,/Assets/icon-64.png")), Width = 52, Height = 52, Margin = new Thickness(0, 0, 16, 0) });
        var ht = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        ht.Children.Add(Eyebrow(kind == WelcomeKind.Upgrade ? "Atualização concluída" : "Boas-vindas", new Thickness(0, 0, 0, 4)));
        ht.Children.Add(new TextBlock
        {
            Text = kind == WelcomeKind.Upgrade ? $"Atualizado para a versão {AppVersion}" : "Bem-vindo ao ARCA Teleprompter",
            FontFamily = (FontFamily)FindResource("FNum"), FontWeight = FontWeights.SemiBold, FontSize = 20, TextWrapping = TextWrapping.Wrap
        });
        ht.Children.Add(Muted(kind == WelcomeKind.Upgrade && !string.IsNullOrEmpty(fromVersion)
            ? $"Da versão {fromVersion} para a {AppVersion}. Seus roteiros e preferências foram mantidos."
            : $"Versão {AppVersion} · teleprompter para estúdio, eventos e jornalismo.", 13));
        head.Children.Add(ht);
        sp.Children.Add(head);

        // Ações rápidas
        sp.Children.Add(Eyebrow("Comece por aqui"));
        var grid = new System.Windows.Controls.Primitives.UniformGrid { Columns = 2, Margin = new Thickness(-4, 0, -4, 14) };
        grid.Children.Add(Card(Icons.New, "Novo roteiro", "Escreva ou cole o texto", () => { CloseDialog(); NewTab(); Editor.Focus(); }));
        grid.Children.Add(Card(Icons.Open, "Abrir roteiro", "TPS, DOCX, PDF, RTF, TXT", () => { CloseDialog(); OpenFileDialog(); }));
        grid.Children.Add(Card(Icons.Bookmark, "Roteiros de exemplo", "Jornal, entrevista, treino", () => { CloseDialog(); OpenSamples(); }));
        grid.Children.Add(Card(Icons.Find, "Manual do operador", "Passo a passo e atalhos", () => { CloseDialog(); Manual_Click(this, new RoutedEventArgs()); }));
        sp.Children.Add(grid);

        if (kind == WelcomeKind.Upgrade)
        {
            // Novidades da versão (docs\NOVIDADES.txt da instalação)
            sp.Children.Add(Eyebrow("Novidades desta versão"));
            var items = ReadNews();
            if (items.Count == 0) sp.Children.Add(Muted("Veja o histórico completo em Ajuda › Manual do operador.", 12));
            foreach (var n in items.Take(8)) sp.Children.Add(Bullet(n));
        }
        else
        {
            // Primeiros passos
            sp.Children.Add(Eyebrow("Primeiros passos"));
            sp.Children.Add(Bullet("Escreva ou cole o roteiro. Uma linha começando com * vira marcador (ex.: *Abertura)."));
            sp.Children.Add(Bullet("Defina quem fala: selecione o trecho e escolha o apresentador (Alt+1, Alt+2…)."));
            sp.Children.Add(Bullet("Em Telas, ligue o monitor do prompter, a saída NDI ou o monitor web para o iPad."));
            sp.Children.Add(Bullet("Espaço roda e pausa; ↑ ↓ ajustam a velocidade; V liga a voz; F5 abre a janela do talento."));
        }

        var bar = new DockPanel { Margin = new Thickness(0, 18, 0, 0), LastChildFill = false };
        var go = Btn(kind == WelcomeKind.Upgrade ? "Continuar" : "Começar", CloseDialog, "Primary"); go.MinWidth = 120; go.Height = 34;
        DockPanel.SetDock(go, Dock.Right); bar.Children.Add(go);
        bar.Children.Add(new TextBlock { Text = "Esta tela fica em Ajuda › Boas-vindas.", Foreground = Br("Faint"), FontSize = 11, VerticalAlignment = VerticalAlignment.Center });
        sp.Children.Add(bar);

        ShowDialog(kind == WelcomeKind.Upgrade ? "Novidades" : "Boas-vindas", 580, Pad(sp, 24));
    }

    private UIElement Card(string icon, string title, string sub, Action click)
    {
        var g = new Grid { Margin = new Thickness(2) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); g.ColumnDefinitions.Add(new ColumnDefinition());
        var ic = new Border { Width = 34, Height = 34, CornerRadius = new CornerRadius(9), Background = Br("AccentSoft"), Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center, Child = new Icon { Data = icon, Size = 16, Foreground = Br("Accent"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
        g.Children.Add(ic);
        var t = new StackPanel { VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(t, 1);
        t.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, FontSize = 13, Foreground = Br("Text") });
        t.Children.Add(new TextBlock { Text = sub, FontSize = 11, Foreground = Br("Muted"), TextTrimming = TextTrimming.CharacterEllipsis });
        g.Children.Add(t);
        var b = new Button { Style = St("Ghost"), Content = g, Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(4), HorizontalContentAlignment = HorizontalAlignment.Stretch, Height = double.NaN, MinHeight = 56, Focusable = false };
        System.Windows.Automation.AutomationProperties.SetName(b, title);
        b.Click += (_, _) => click();
        return new Border { BorderBrush = Br("Line"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Margin = new Thickness(4), Child = b };
    }

    private UIElement Bullet(string text)
    {
        var g = new Grid { Margin = new Thickness(0, 0, 0, 7) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) }); g.ColumnDefinitions.Add(new ColumnDefinition());
        g.Children.Add(new Ellipse(Br("Accent")));
        var t = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 12.5, Foreground = Br("Text"), LineHeight = 18 }; Grid.SetColumn(t, 1);
        g.Children.Add(t);
        return g;
    }

    private sealed class Ellipse : Border
    {
        public Ellipse(Brush b) { Width = 6; Height = 6; CornerRadius = new CornerRadius(3); Background = b; VerticalAlignment = VerticalAlignment.Top; HorizontalAlignment = HorizontalAlignment.Left; Margin = new Thickness(2, 6, 0, 0); }
    }

    /// <summary>Itens "- …" do NOVIDADES.txt instalado (pasta docs do programa).</summary>
    private static List<string> ReadNews()
    {
        var list = new List<string>();
        foreach (var f in new[] { Path.Combine(AppContext.BaseDirectory, "docs", "NOVIDADES.txt"), Path.Combine(AppContext.BaseDirectory, "..", "docs", "NOVIDADES.txt") })
        {
            if (!File.Exists(f)) continue;
            foreach (var l in File.ReadAllLines(f)) if (l.StartsWith("- ")) list.Add(l[2..].Trim());
            break;
        }
        return list;
    }

    /// <summary>Abre a pasta de exemplos (Documentos\ARCA Teleprompter\Exemplos) no diálogo de abrir.</summary>
    private void OpenSamples()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ARCA Teleprompter", "Exemplos");
        var alt = Path.Combine(AppContext.BaseDirectory, "samples");
        var d = new Microsoft.Win32.OpenFileDialog { Title = "Roteiros de exemplo", Filter = "Roteiros|*.tps|Todos os arquivos|*.*", InitialDirectory = Directory.Exists(dir) ? dir : Directory.Exists(alt) ? alt : null };
        if (d.ShowDialog(this) == true) OpenPath(d.FileName);
    }

    private void Welcome_Click(object s, RoutedEventArgs e) => ShowWelcome(WelcomeKind.Manual);
    private async void CheckUpdates_Click(object s, RoutedEventArgs e) => await Updates.Check(manual: true);
}
