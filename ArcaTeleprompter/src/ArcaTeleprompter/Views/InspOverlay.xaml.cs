using System.Windows;
using System.Windows.Controls;
using Arca.App.ViewModels;
using Microsoft.Win32;

namespace Arca.App.Views;

/// <summary>Inspetor › Overlay: faixa de leitura, indicador, linha de leitura e imagem de fundo.</summary>
public partial class InspOverlay : UserControl
{
    private MainViewModel Vm => (MainViewModel)DataContext;
    public static readonly string[] IndPresets = { "#ffffff", "#ffd54a", "#ff6b6b", "#6aa6dc" };

    public InspOverlay()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is not MainViewModel vm) return;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IndicatorColor)) Build();
                if (e.PropertyName == nameof(MainViewModel.BgImagePath)) ClearBgBtn.Visibility = vm.BgImagePath == null ? Visibility.Collapsed : Visibility.Visible;
            };
            ClearBgBtn.Visibility = vm.BgImagePath == null ? Visibility.Collapsed : Visibility.Visible;
            Build();
        };
    }

    private void Build() => InspRoteiro.Swatches(IndSwatches, IndPresets, Vm.IndicatorColor, v => Vm.IndicatorColor = v);

    private void PickBg_Click(object sender, RoutedEventArgs e)
    {
        var d = new OpenFileDialog { Title = "Imagem de fundo", Filter = "Imagens|*.png;*.jpg;*.jpeg;*.webp;*.bmp;*.gif;*.svg|Todos os arquivos|*.*" };
        if (d.ShowDialog(MainWindow.Instance) == true) Vm.BgImagePath = d.FileName;
    }
    private void ClearBg_Click(object sender, RoutedEventArgs e) => Vm.BgImagePath = null;
}
