using System.Text.Json.Serialization;

namespace Arca.Core.Model;

/// <summary>Apresentador: nome e cor (#rrggbb).</summary>
public sealed record Presenter(string Name, string Color)
{
    public override string ToString() => Name;
}

/// <summary>
/// Aparência e rolagem do prompter. Nomes iguais aos campos do .tps (settings) para compatibilidade.
/// </summary>
public sealed class PrompterSettings
{
    [JsonPropertyName("wpl")] public double WordsPerLine { get; set; } = 5;
    [JsonPropertyName("margin")] public double Margin { get; set; } = 0;
    /// <summary>Posição da margem: −1 = esquerda, 0 = centro, +1 = direita.</summary>
    [JsonPropertyName("mPos")] public double MarginPos { get; set; } = -1;
    [JsonPropertyName("lineH")] public double LineHeight { get; set; } = 1.3;
    /// <summary>Espaço entre parágrafos, em frações da altura da linha (0,25 = um quarto de linha).</summary>
    [JsonPropertyName("paraGap")] public double ParagraphGap { get; set; } = 0.25;
    /// <summary>atk, sans, serif, mono ou "sys:Família" (fonte instalada no Windows).</summary>
    [JsonPropertyName("fontKey")] public string FontKey { get; set; } = "atk";
    [JsonPropertyName("bg")] public string Background { get; set; } = "#000000";
    [JsonPropertyName("fg")] public string Foreground { get; set; } = "#ffffff";
    [JsonPropertyName("align")] public string Align { get; set; } = "left";
    [JsonPropertyName("readPct")] public double ReadPct { get; set; } = 30;
    /// <summary>faixa, fade, destaque ou nenhum.</summary>
    [JsonPropertyName("ovType")] public string OverlayType { get; set; } = "nenhum";
    [JsonPropertyName("ovOp")] public double OverlayOpacity { get; set; } = 0.55;
    /// <summary>seta, quadrado, ponto, linha ou nenhum.</summary>
    [JsonPropertyName("indType")] public string IndicatorType { get; set; } = "seta";
    /// <summary>0 = P, 1 = M, 2 = G.</summary>
    [JsonPropertyName("indSize")] public int IndicatorSize { get; set; } = 1;
    [JsonPropertyName("indColor")] public string IndicatorColor { get; set; } = "#ffffff";
    [JsonPropertyName("readingLine")] public bool ReadingLine { get; set; }
    [JsonPropertyName("speedT")] public double SpeedT { get; set; } = 0.15;
    [JsonPropertyName("minS")] public double MinSpeed { get; set; } = 0.2;
    [JsonPropertyName("maxS")] public double MaxSpeed { get; set; } = 2.5;
    [JsonPropertyName("pres")] public List<Presenter> Presenters { get; set; } = new() { new("Âncora", "#ffd60a"), new("Repórter", "#5ac8fa") };
    [JsonPropertyName("presNames")] public bool PresenterNames { get; set; } = true;
    /// <summary>"0" = todas as falas; "N" = destaca só o apresentador N.</summary>
    [JsonPropertyName("presOnly")] public string PresenterOnly { get; set; } = "0";
    [JsonPropertyName("progSec")] public int ProgramSeconds { get; set; } = 600;
    [JsonPropertyName("bgImgOp")] public double BackgroundImageOpacity { get; set; } = 0.2;

    public static readonly string[] DefaultPresenterColors = { "#ffd60a", "#5ac8fa", "#ff9f0a", "#a2e05c", "#ff6b9d", "#bf9bff", "#4cd9c4", "#ff8a65" };

    /// <summary>Copia todos os valores de outro objeto (mantém a mesma instância, usada pelo motor).</summary>
    public void CopyFrom(PrompterSettings o)
    {
        foreach (var pr in typeof(PrompterSettings).GetProperties().Where(x => x.CanWrite)) pr.SetValue(this, pr.GetValue(o));
        Presenters = o.Presenters.ToList();
    }

    public PrompterSettings Clone() { var c = (PrompterSettings)MemberwiseClone(); c.Presenters = Presenters.ToList(); return c; }

    /// <summary>Linhas por segundo na velocidade atual.</summary>
    public double LinesPerSecond => MinSpeed + SpeedT * (MaxSpeed - MinSpeed);

    /// <summary>
    /// Padrões antigos de velocidade (0,3 → 3 linhas/s, início em 1,11 linha/s ≈ 330 palavras/min, rápido demais) viram
    /// os novos (0,2 → 2,5, início em 0,55 linha/s ≈ 165 palavras/min). Velocidades escolhidas pelo usuário não mudam.
    /// </summary>
    public PrompterSettings MigrateSpeed()
    {
        if (Math.Abs(SpeedT - 0.3) < 1e-6 && Math.Abs(MinSpeed - 0.3) < 1e-6 && Math.Abs(MaxSpeed - 3) < 1e-6) { SpeedT = 0.15; MinSpeed = 0.2; MaxSpeed = 2.5; }
        return this;
    }
}
