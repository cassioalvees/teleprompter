using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace Arca.App.Controls;

/// <summary>
/// Barra de controle do prompter — a mesma em todas as telas (janela do talento, janelas de prompter e, no navegador,
/// o modo controle): parágrafo ↑, linha ↑, linha ↓, parágrafo ↓ · play · início · ré │ − velocidade + e o valor.
/// Os botões usam os mesmos comandos do controle remoto (<see cref="MainWindow.RemoteCommand"/>).
/// </summary>
public sealed class PrompterControlBar : Border
{
    /// <summary>Controle de velocidade (desabilitado quando a velocidade está travada).</summary>
    public Slider Speed { get; }

    public PrompterControlBar()
    {
        var vm = MainWindow.Instance.Vm;
        DataContext = vm;
        VerticalAlignment = VerticalAlignment.Bottom; HorizontalAlignment = HorizontalAlignment.Center;
        Margin = new Thickness(0, 0, 0, 20); Padding = new Thickness(6);
        CornerRadius = new CornerRadius(12);
        Background = new SolidColorBrush(Color.FromArgb(0xEB, 0x1E, 0x1E, 0x1E));
        BorderBrush = new SolidColorBrush(Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF)); BorderThickness = new Thickness(1);

        var row = new StackPanel { Orientation = Orientation.Horizontal };
        Button B(string icon, string tip, string cmd, double size = 15)
        {
            var b = new Button { Style = (Style)Application.Current.FindResource("TalentBtn"), ToolTip = tip, Focusable = false, Content = new Icon { Data = icon, Size = size, StrokeWidth = 1.4 } };
            b.Click += (_, _) => MainWindow.Instance.RemoteCommand(cmd, null);
            return b;
        }
        Button T(string text, string tip, string cmd)
        {
            var b = new Button { Style = (Style)Application.Current.FindResource("TalentBtn"), ToolTip = tip, Focusable = false, Content = new TextBlock { Text = text, FontSize = 18, FontWeight = FontWeights.SemiBold } };
            b.Click += (_, _) => MainWindow.Instance.RemoteCommand(cmd, null);
            return b;
        }
        row.Children.Add(B(Icons.ParaUp, "Parágrafo anterior ([)", "prevPara"));
        row.Children.Add(B(Icons.Up, "Linha anterior", "lineUp"));
        row.Children.Add(B(Icons.Down, "Próxima linha", "lineDown"));
        row.Children.Add(B(Icons.ParaDown, "Próximo parágrafo (])", "nextPara"));

        var play = new Button { Width = 52, Height = 36, Padding = new Thickness(0), Focusable = false, ToolTip = "Play / pausa (Espaço)", Margin = new Thickness(2, 0, 2, 0) };
        var playStyle = new Style(typeof(Button), (Style)Application.Current.FindResource("BtnBase"));
        playStyle.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xE6, 0xE6, 0xE6))));
        playStyle.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E))));
        var live = new DataTrigger { Binding = new Binding("Live"), Value = true };
        live.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xE5, 0x48, 0x4D))));
        live.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
        playStyle.Triggers.Add(live);
        play.Style = playStyle;
        var icon = new Icon { Data = Icons.Play, Size = 14, Filled = true };
        play.Content = icon;
        vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.Live)) icon.Data = vm.Live ? Icons.Pause : Icons.Play; };
        icon.Data = vm.Live ? Icons.Pause : Icons.Play;
        play.Click += (_, _) => MainWindow.Instance.RemoteCommand("togglePlay", null);
        row.Children.Add(play);

        row.Children.Add(B(Icons.Rewind, "Voltar ao início", "rewind", 14));
        row.Children.Add(B(Icons.Direction, "Inverter direção (R)", "reverse", 14));
        row.Children.Add(new Border { Width = 1, Height = 20, Background = new SolidColorBrush(Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF)), Margin = new Thickness(6, 0, 6, 0) });
        row.Children.Add(T("−", "Mais devagar", "slower"));
        var slider = Speed = new Slider { Width = 140, Minimum = 0, Maximum = 1, Focusable = false, VerticalAlignment = VerticalAlignment.Center };
        slider.SetBinding(RangeBase.ValueProperty, new Binding("SpeedT") { Mode = BindingMode.TwoWay });
        row.Children.Add(slider);
        row.Children.Add(T("+", "Mais rápido", "faster"));
        var label = new TextBlock { FontFamily = (FontFamily)Application.Current.FindResource("FNum"), FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xE6, 0xE6)), MinWidth = 84, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) };
        var r1 = new Run(); r1.SetBinding(Run.TextProperty, new Binding("SpeedValue") { Mode = BindingMode.OneWay });
        var r2 = new Run(); r2.SetBinding(Run.TextProperty, new Binding("UnitName") { Mode = BindingMode.OneWay });
        label.Inlines.Add(r1); label.Inlines.Add(new Run(" ")); label.Inlines.Add(r2);
        row.Children.Add(label);
        Child = row;
    }
}
