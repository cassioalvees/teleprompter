using Arca.Core.Model;

namespace Arca.Core.Engine;

/// <summary>Unidade de exibição da velocidade.</summary>
public enum SpeedUnit { LinesPerSecond, LinesPerMinute, WordsPerSecond, WordsPerMinute }

/// <summary>
/// Motor de rolagem, independente de interface. A posição <see cref="Y"/> é medida no espaço de
/// 1000 unidades (topo do texto = 0). O app chama <see cref="Tick"/> a cada quadro da tela
/// (CompositionTarget.Rendering), então a rolagem acompanha o refresh do monitor.
/// </summary>
public sealed class ScrollEngine
{
    private readonly PrompterSettings _s;
    public ScrollEngine(PrompterSettings settings) => _s = settings;

    public double Y { get; private set; }
    /// <summary>Velocidade aplicada no último quadro, em unidades/s (para as saídas extrapolarem).</summary>
    public double Velocity { get; private set; }
    private double _smoothV;
    /// <summary>Constante de tempo da rampa de velocidade (s). 0 = mudança instantânea.</summary>
    public double Ramp { get; set; } = 0.35;
    private double _rampTarget = double.NaN, _rampRate;

    /// <summary>
    /// Velocidade com aceleração constante até o alvo: cada mudança se completa em <see cref="Ramp"/> segundos, com a
    /// velocidade variando em linha reta (sem o "puxão" do início de uma curva exponencial). Novo toque recalcula.
    /// </summary>
    private double Approach(double target, double dt)
    {
        if (Ramp <= 0) return _smoothV = target;
        if (double.IsNaN(_rampTarget) || Math.Abs(target - _rampTarget) > 1e-6)
        {
            _rampTarget = target;
            _rampRate = Math.Max(Math.Abs(target - _smoothV) / Ramp, Math.Max(LineHeight, 1) * 0.5);
        }
        var step = _rampRate * dt;
        _smoothV = Math.Abs(target - _smoothV) <= step ? target : _smoothV + Math.Sign(target - _smoothV) * step;
        return _smoothV;
    }
    public bool Playing { get; private set; }
    /// <summary>+1 = frente, −1 = ré.</summary>
    public int Direction { get; private set; } = 1;
    /// <summary>Botões "segure para avançar/recuar": −1, 0 ou +1.</summary>
    public int Hold { get; set; }
    /// <summary>Contagem regressiva em andamento (3, 2, 1) ou 0.</summary>
    public int Countdown { get; private set; }
    /// <summary>Quando ligado, a velocidade não muda pela janela do talento.</summary>
    public bool SpeedLock { get; set; }

    /// <summary>Altura de uma linha em unidades (fonte × entrelinha).</summary>
    public double LineHeight { get; set; } = 60;
    /// <summary>Última posição alcançável (topo da última linha).</summary>
    public double EndY { get; set; }
    /// <summary>Posições Y das pausas, em ordem.</summary>
    public IReadOnlyList<double> Stops { get; set; } = Array.Empty<double>();
    /// <summary>Alvo vindo do acompanhamento por voz (null = sem voz).</summary>
    public double? VoiceTarget { get; set; }
    public bool VoiceMode { get; set; }
    /// <summary>Tempo (s) para a rolagem por voz alcançar o alvo — maior = mais suave.</summary>
    public double VoiceSmooth { get; set; } = 0.35;
    private double _voiceV;

    public event Action? PlayStateChanged;
    public event Action<double>? StopReached;
    public event Action<double, int>? SpeedChanged;

    public double LinesPerSecond => _s.LinesPerSecond;

    public void Play() { if (Playing) return; Playing = true; Countdown = 0; PlayStateChanged?.Invoke(); }
    public void Pause() { if (!Playing && Countdown == 0 && Hold == 0) return; Playing = false; Countdown = 0; Hold = 0; PlayStateChanged?.Invoke(); }
    public void SetCountdown(int n) { Countdown = n; PlayStateChanged?.Invoke(); }
    public bool Live => Playing || Countdown > 0;

    public void SetY(double y) { Y = Math.Clamp(y, 0, Math.Max(0, EndY)); VoiceTarget = null; _voiceV = 0; }
    public void Nudge(double dy) => SetY(Y + dy);
    public void Reverse() { Direction = -Direction; SpeedChanged?.Invoke(_s.SpeedT, Direction); }
    public void SetDirection(int d) { Direction = d >= 0 ? 1 : -1; }

    /// <summary>
    /// Mais rápido (+) / mais devagar (−) no sentido atual. Para na velocidade mínima — a ré é só pelo botão/tecla
    /// de inverter (passar por zero ao frear atrapalhava a leitura).
    /// </summary>
    public bool AdjustSpeed(double delta)
    {
        if (SpeedLock) return false;
        _s.SpeedT = Math.Clamp(_s.SpeedT + delta, 0, 1);
        SpeedChanged?.Invoke(_s.SpeedT, Direction);
        return true;
    }

    /// <summary>Velocidades predefinidas 1–9.</summary>
    public void Preset(int n) { _s.SpeedT = Math.Clamp((n - 1) / 8.0, 0, 1); SpeedChanged?.Invoke(_s.SpeedT, Direction); }

    /// <summary>Avança um quadro. <paramref name="dt"/> em segundos (limitado a 0,25 s).</summary>
    public void Tick(double dt)
    {
        // Até 0,25 s: se a interface travar um instante, o texto alcança o tempo real (o NDI e o navegador já preveem
        // a posição até 0,25 s à frente — com limite menor, o texto voltava um pouco nessas telas depois da trava).
        dt = Math.Clamp(dt, 0, 0.25);
        var y = Y; double vel = 0;
        if (VoiceMode)
        {
            // Aproximação suave do alvo (constante de tempo ~0,3 s), com velocidade limitada para não "pular".
            // Mola criticamente amortecida: começa e termina o movimento com suavidade (sem o arranco de uma
            // aproximação exponencial) e nunca passa do alvo; velocidade máxima de 3 linhas/s.
            if (VoiceTarget is { } t)
            {
                var w = 2.2 / Math.Max(0.1, VoiceSmooth);
                var acc = w * w * (t - y) - 2 * w * _voiceV;
                // Aceleração limitada (6 linhas/s²): a palavra nova não dá "tranco" no início do movimento.
                var amax = Math.Max(LineHeight, 1) * 6;
                acc = Math.Clamp(acc, -amax, amax);
                _voiceV += acc * dt;
                var vmax = Math.Max(LineHeight, 1) * 3;
                _voiceV = Math.Clamp(_voiceV, -vmax, vmax);
                vel = _voiceV; y += _voiceV * dt;
            }
            else _voiceV = 0;
        }
        else if (Playing)
        {
            // Rampa: a velocidade desliza até o novo valor (constante de tempo ≈ 0,22 s) — acelerar, frear, inverter e
            // dar play ficam fluidos, sem trancos a cada toque de tecla.
            var target = (LinesPerSecond * Direction + Hold * LinesPerSecond * 2) * LineHeight;
            vel = Approach(target, dt);
            var ny = y + vel * dt;
            if (vel > 0)
            {
                foreach (var st in Stops)
                {
                    var stopAt = st - LineHeight * 0.2;
                    if (stopAt > y + 0.5 && stopAt <= ny) { y = stopAt; vel = 0; Playing = false; PlayStateChanged?.Invoke(); StopReached?.Invoke(stopAt); goto clamp; }
                }
            }
            y = ny;
        }
        else if (Hold != 0) { vel = Approach(Hold * LinesPerSecond * 3 * LineHeight, dt); y += vel * dt; }
        else { _smoothV = 0; _rampTarget = double.NaN; }
        clamp:
        var end = Math.Max(0, EndY);
        y = Math.Clamp(y, 0, end);
        if (Playing && !VoiceMode && ((y >= end && Direction > 0) || (y <= 0 && Direction < 0))) { Playing = false; PlayStateChanged?.Invoke(); }
        if (y <= 0 || y >= end) vel = 0;
        Y = y; Velocity = vel;
    }

    /// <summary>Segundos restantes até o fim na velocidade atual.</summary>
    public double SecondsLeft => (EndY - Y) / Math.Max(1e-6, LinesPerSecond * LineHeight);
    public double SecondsAt(double y) => y / Math.Max(1e-6, LinesPerSecond * LineHeight);

    /// <summary>Velocidade formatada na unidade escolhida.</summary>
    public static (string Value, string Unit) Format(double linesPerSecond, SpeedUnit unit, double wordsPerLine) => unit switch
    {
        SpeedUnit.LinesPerMinute => ((linesPerSecond * 60).ToString("0"), "linhas/min"),
        SpeedUnit.WordsPerSecond => ((linesPerSecond * wordsPerLine).ToString("0.0"), "palavras/s"),
        SpeedUnit.WordsPerMinute => ((linesPerSecond * wordsPerLine * 60).ToString("0"), "palavras/min"),
        _ => (linesPerSecond.ToString("0.00"), "linhas/s")
    };

    /// <summary>
    /// Duração automática: calcula a velocidade (speedT) que termina o texto em <paramref name="seconds"/>.
    /// </summary>
    public double SpeedForDuration(double seconds)
    {
        if (seconds <= 0) return _s.SpeedT;
        var need = (EndY / Math.Max(1, LineHeight)) / seconds;
        return Math.Clamp((need - _s.MinSpeed) / Math.Max(1e-6, _s.MaxSpeed - _s.MinSpeed), 0, 1);
    }
}
