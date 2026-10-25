using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Arca.App.Editor;
using Arca.Core.Model;
using Arca.Core.Storage;

namespace Arca.App.ViewModels;

/// <summary>Apresentador editável no inspetor.</summary>
public sealed partial class PresenterVM : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _color = "#ffd60a";
    [ObservableProperty] private int _number;
    public string Key => "Alt+" + Number;
}

/// <summary>Linha da lista de marcadores (rundown).</summary>
public sealed partial class BookmarkVM : ObservableObject
{
    public int Index { get; init; }          // ordem entre os marcadores (0…)
    public int Block { get; init; }          // índice no documento
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _sub = "";
    [ObservableProperty] private bool _skip;
    [ObservableProperty] private bool _current;
    [ObservableProperty] private bool _dropTarget;
    public string Key => Index < 9 ? (Index + 1).ToString() : "";
}

/// <summary>Linha da lista de pausas.</summary>
public sealed partial class StopVM : ObservableObject
{
    public int Index { get; init; }
    public int Block { get; init; }
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _time = "";
}

/// <summary>Aba de roteiro aberto.</summary>
public sealed partial class DocTabVM : ObservableObject
{
    public long Id { get; init; }
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private bool _dirty;
    [ObservableProperty] private bool _active;
    public string? Path { get; set; }
    public ScriptDocument Doc { get; set; } = new();
    public double Y { get; set; }
    public PrompterSettings? Settings { get; set; }
}

/// <summary>Tela de saída (monitor ou janela).</summary>
public sealed partial class DisplayVM : ObservableObject
{
    public string Id { get; init; } = "";
    public string DeviceName { get; init; } = "";
    public bool Operator { get; init; }
    public bool Windowed { get; init; }
    public System.Windows.Int32Rect Bounds { get; set; }
    [ObservableProperty] private string _nick = "";
    [ObservableProperty] private string _desc = "";
    [ObservableProperty] private bool _on;
    [ObservableProperty] private bool _flipH;
    [ObservableProperty] private bool _flipV;
    [ObservableProperty] private string _align = "left";
    [ObservableProperty] private bool _find;
}

/// <summary>Palavra ouvida pelo acompanhamento por voz.</summary>
public sealed record HeardVM(string Word, int Match);

/// <summary>Leitura da barra de transporte.</summary>
public sealed partial class ReadoutVM : ObservableObject
{
    [ObservableProperty] private string _label = "";
    [ObservableProperty] private string _value = "";
}

/// <summary>
/// Estado da janela principal. As propriedades de aparência espelham <see cref="PrompterSettings"/>; qualquer
/// mudança dispara <see cref="LookChanged"/> (o editor e as saídas são rediagramados).
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    public AppSettings App { get; }
    public PrompterSettings S => App.Prompter;
    private bool _loading;

    public MainViewModel(AppSettings app)
    {
        App = app;
        _loading = true;
        LoadFromSettings();
        _loading = false;
        foreach (var o in FontCatalog.Bundled) Fonts.Add(o);
    }

    /// <summary>Aparência mudou: rediagramar (layout=true) ou só repintar.</summary>
    public event Action<bool>? LookChanged;

    public void LoadFromSettings()
    {
        var s = S;
        _loading = true;
        Background = s.Background; Foreground = s.Foreground; FontKey = s.FontKey; Align = s.Align;
        WordsPerLine = s.WordsPerLine; Margin = s.Margin; MarginPos = s.MarginPos; LineHeight = s.LineHeight; ParagraphGap = s.ParagraphGap;
        MinSpeed = s.MinSpeed; MaxSpeed = s.MaxSpeed; SpeedT = s.SpeedT;
        OverlayType = s.OverlayType; OverlayOpacity = s.OverlayOpacity; IndicatorType = s.IndicatorType; IndicatorSize = s.IndicatorSize;
        IndicatorColor = s.IndicatorColor; ReadPct = s.ReadPct; ReadingLine = s.ReadingLine; PresenterNames = s.PresenterNames; PresenterOnly = s.PresenterOnly;
        BgImageOpacity = s.BackgroundImageOpacity; ProgramInput = Arca.Core.Model.TextUtil.FormatMmSs(s.ProgramSeconds);
        Presenters.Clear();
        for (var i = 0; i < s.Presenters.Count; i++) AddPresenterVM(s.Presenters[i].Name, s.Presenters[i].Color);
        _loading = false;
    }

    private void Sync(bool layout)
    {
        if (_loading) return;
        var s = S;
        s.Background = Background; s.Foreground = Foreground; s.FontKey = FontKey; s.Align = Align;
        s.WordsPerLine = WordsPerLine; s.Margin = Margin; s.MarginPos = MarginPos; s.LineHeight = LineHeight; s.ParagraphGap = ParagraphGap;
        s.MinSpeed = MinSpeed; s.MaxSpeed = MaxSpeed; s.SpeedT = SpeedT;
        s.OverlayType = OverlayType; s.OverlayOpacity = OverlayOpacity; s.IndicatorType = IndicatorType; s.IndicatorSize = IndicatorSize;
        s.IndicatorColor = IndicatorColor; s.ReadPct = ReadPct; s.ReadingLine = ReadingLine; s.PresenterNames = PresenterNames; s.PresenterOnly = PresenterOnly;
        s.BackgroundImageOpacity = BgImageOpacity;
        s.Presenters = Presenters.Select(p => new Presenter(p.Name, p.Color)).ToList();
        LookChanged?.Invoke(layout);
    }

    // ——— Aparência ———
    [ObservableProperty] private string _background = "#000000";
    [ObservableProperty] private string _foreground = "#ffffff";
    [ObservableProperty] private string _fontKey = "atk";
    [ObservableProperty] private string _align = "left";
    [ObservableProperty] private bool _edFlipH;
    [ObservableProperty] private bool _edFlipV;
    [ObservableProperty] private double _wordsPerLine = 5;
    [ObservableProperty] private double _margin;
    [ObservableProperty] private double _marginPos = -1;
    [ObservableProperty] private double _lineHeight = 1.3;
    [ObservableProperty] private double _paragraphGap = 0.25;
    [ObservableProperty] private double _minSpeed = 0.3;
    [ObservableProperty] private double _maxSpeed = 3;
    [ObservableProperty] private double _speedT = 0.3;
    [ObservableProperty] private string _overlayType = "nenhum";
    [ObservableProperty] private double _overlayOpacity = 0.55;
    [ObservableProperty] private string _indicatorType = "seta";
    [ObservableProperty] private int _indicatorSize = 1;
    [ObservableProperty] private string _indicatorColor = "#ffffff";
    [ObservableProperty] private double _readPct = 30;
    [ObservableProperty] private bool _readingLine;
    [ObservableProperty] private bool _presenterNames = true;
    [ObservableProperty] private string _presenterOnly = "0";
    [ObservableProperty] private string? _bgImagePath;
    [ObservableProperty] private double _bgImageOpacity = 0.2;

    partial void OnBackgroundChanged(string value) => Sync(false);
    partial void OnForegroundChanged(string value) => Sync(true);
    partial void OnFontKeyChanged(string value) => Sync(true);
    partial void OnAlignChanged(string value) => Sync(true);
    partial void OnEdFlipHChanged(bool value) => LookChanged?.Invoke(false);
    partial void OnEdFlipVChanged(bool value) => LookChanged?.Invoke(false);
    partial void OnWordsPerLineChanged(double value) { OnPropertyChanged(nameof(WplLabel)); Sync(true); }
    partial void OnMarginChanged(double value) => Sync(true);
    partial void OnMarginPosChanged(double value) { OnPropertyChanged(nameof(MarginPosLabel)); Sync(true); }
    partial void OnLineHeightChanged(double value) => Sync(true);
    partial void OnParagraphGapChanged(double value) => Sync(true);
    partial void OnMinSpeedChanged(double value) { if (!_loading && value > MaxSpeed - 0.2) MinSpeed = Math.Round(MaxSpeed - 0.2, 1); Sync(false); }
    partial void OnMaxSpeedChanged(double value) { if (!_loading && value < MinSpeed + 0.2) MaxSpeed = Math.Round(MinSpeed + 0.2, 1); Sync(false); }
    // Velocidade não é aparência: só atualiza o valor (antes reaplicava o visual de todas as telas a cada toque,
    // travando a rolagem por um instante bem na mudança de velocidade).
    partial void OnSpeedTChanged(double value) { if (!_loading) S.SpeedT = value; }
    partial void OnOverlayTypeChanged(string value) => Sync(false);
    partial void OnOverlayOpacityChanged(double value) => Sync(false);
    partial void OnIndicatorTypeChanged(string value) => Sync(true);
    partial void OnIndicatorSizeChanged(int value) => Sync(true);
    partial void OnIndicatorColorChanged(string value) => Sync(false);
    partial void OnReadPctChanged(double value) => Sync(false);
    partial void OnReadingLineChanged(bool value) => Sync(false);
    partial void OnPresenterNamesChanged(bool value) => Sync(true);
    partial void OnPresenterOnlyChanged(string value) => Sync(true);
    partial void OnBgImagePathChanged(string? value) => LookChanged?.Invoke(false);
    partial void OnBgImageOpacityChanged(double value) => Sync(false);

    public string WplLabel => WordsPerLine.ToString("0.##");
    public string MarginPosLabel => MarginPos <= -0.95 ? "Esq." : MarginPos >= 0.95 ? "Dir." : Math.Abs(MarginPos) < 0.05 ? "Centro" : (MarginPos < 0 ? "E " : "D ") + Math.Round(Math.Abs(MarginPos) * 100) + "%";

    public ObservableCollection<FontOption> Fonts { get; } = new();
    public ObservableCollection<PresenterVM> Presenters { get; } = new();

    public void AddPresenterVM(string name, string color)
    {
        var p = new PresenterVM { Name = name, Color = color, Number = Presenters.Count + 1 };
        p.PropertyChanged += PresenterChanged;
        Presenters.Add(p);
    }
    private void PresenterChanged(object? s, PropertyChangedEventArgs e) => Sync(true);
    public void RenumberPresenters() { for (var i = 0; i < Presenters.Count; i++) Presenters[i].Number = i + 1; Sync(true); }

    // ——— Velocidade ———
    [ObservableProperty] private int _speedUnit;
    [ObservableProperty] private string _speedValue = "";
    [ObservableProperty] private string _unitName = "linhas/s";
    [ObservableProperty] private string _minLabel = "";
    [ObservableProperty] private string _maxLabel = "";
    [ObservableProperty] private bool _reverse;
    [ObservableProperty] private bool _autoRun;
    [ObservableProperty] private string _runTarget = "01:30";
    [ObservableProperty] private string _totalLabel = "";
    partial void OnSpeedUnitChanged(int value) { App.SpeedUnit = value; LookChanged?.Invoke(false); }

    // ——— Reprodução / leituras ———
    [ObservableProperty] private bool _playing;
    [ObservableProperty] private bool _live;
    [ObservableProperty] private bool _locked;
    [ObservableProperty] private bool _talent;
    [ObservableProperty] private bool _follow = true;
    [ObservableProperty] private int _count;
    public ObservableCollection<ReadoutVM> Readouts { get; } = new()
    {
        new() { Label = "POSIÇÃO" }, new() { Label = "DURAÇÃO" }, new() { Label = "RELÓGIO" }, new() { Label = "FIM PREVISTO" }, new() { Label = "LINHA" }
    };
    [ObservableProperty] private int _stampMode;

    // ——— Documento e rundown ———
    public ObservableCollection<DocTabVM> Tabs { get; } = new();
    public ObservableCollection<BookmarkVM> Bookmarks { get; } = new();
    public ObservableCollection<StopVM> Stops { get; } = new();
    [ObservableProperty] private string _docTitle = "";
    [ObservableProperty] private int _words;
    [ObservableProperty] private string _rundownTotal = "00:00";
    [ObservableProperty] private string _overLabel = "";
    [ObservableProperty] private bool _over;
    [ObservableProperty] private string _overTitle = "";
    [ObservableProperty] private string _programInput = "10:00";
    [ObservableProperty] private bool _rehearse;
    [ObservableProperty] private bool _hasRehearsal;
    [ObservableProperty] private string _savedAt = "";
    [ObservableProperty] private string _lastKey = "";
    [ObservableProperty] private int _currentWho;
    [ObservableProperty] private string _curPresName = "Sem apresentador";
    [ObservableProperty] private string _curPresColor = "Transparent";

    // ——— Inspetor ———
    [ObservableProperty] private string _tab = "roteiro";
    [ObservableProperty] private bool _showBookmarks = true;
    [ObservableProperty] private bool _showInspector = true;
    partial void OnShowBookmarksChanged(bool value) => App.ShowBookmarks = value;
    partial void OnShowInspectorChanged(bool value) => App.ShowInspector = value;

    // ——— Telas ———
    public ObservableCollection<DisplayVM> Displays { get; } = new();
    [ObservableProperty] private string _previewLabel = "SAÍDA · JANELA FLUTUANTE";
    [ObservableProperty] private bool _talentHud = true;

    // ——— NDI ———
    [ObservableProperty] private bool _ndiOn;
    [ObservableProperty] private string _ndiStatus = "Desligada";
    [ObservableProperty] private string _ndiDot = "Faint";
    // ——— Monitor web ———
    [ObservableProperty] private bool _webOn;
    [ObservableProperty] private string _webStatus = "Desligado";
    [ObservableProperty] private string _webUrl = "";
    [ObservableProperty] private string _webUrlHttps = "";
    [ObservableProperty] private string _webDot = "Faint";
    [ObservableProperty] private int _webViewers;
    // ——— MOS / legenda ———
    [ObservableProperty] private bool _mosOn;
    [ObservableProperty] private string _mosStatus = "Desconectado";
    [ObservableProperty] private string _captionStatus = "Desligado";

    // ——— Voz ———
    [ObservableProperty] private string _voiceMode = "off";
    [ObservableProperty] private string _voiceCode = "DESLIGADO";
    [ObservableProperty] private string _voiceTitle = "Acompanhamento desligado";
    [ObservableProperty] private string _voiceShort = "desligada";
    [ObservableProperty] private string _voiceError = "";
    public ObservableCollection<HeardVM> Heard { get; } = new();
    public ObservableCollection<string> AudioDevices { get; } = new();
    public ObservableCollection<string> AsioChannels { get; } = new();
    [ObservableProperty] private double _levelDb = -70;
    [ObservableProperty] private string _modelStatus = "";
    [ObservableProperty] private bool _modelMissing;
    [ObservableProperty] private double _modelProgress;
    [ObservableProperty] private bool _useScriptVocabulary = true;

    // ——— Vídeo ———
    [ObservableProperty] private bool _camOn;
    [ObservableProperty] private string _camMessage = "Câmera desligada";
    [ObservableProperty] private bool _recording;
    [ObservableProperty] private string _recLabel = "00:00";
    [ObservableProperty] private string? _lastRecording;

    // ——— Status ———
    [ObservableProperty] private string _statusLabel = "Pronto";
    [ObservableProperty] private string _screensLabel = "0 tela(s) de prompter";
    [ObservableProperty] private string _netLabel = "";
    [ObservableProperty] private string _flashText = "";
    [ObservableProperty] private string _flashSub = "";
    [ObservableProperty] private string _theme = "light";
}
