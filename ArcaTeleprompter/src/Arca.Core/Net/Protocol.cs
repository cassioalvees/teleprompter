using System.Text.Json.Serialization;

namespace Arca.Core.Net;

/// <summary>
/// Estado do prompter enviado ao monitor web / PWA (mensagem "state"). O texto vai separado
/// (mensagem "html") e a posição em mensagens "y" a cada quadro. Ver docs/05-protocolos.md.
/// </summary>
public sealed class PrompterSnapshot
{
    [JsonPropertyName("bg")] public string Bg { get; set; } = "#000000";
    [JsonPropertyName("fg")] public string Fg { get; set; } = "#ffffff";
    [JsonPropertyName("fontFamily")] public string FontFamily { get; set; } = "Atkinson Hyperlegible";
    [JsonPropertyName("fontSize")] public double FontSize { get; set; }
    [JsonPropertyName("lineH")] public double LineH { get; set; }
    [JsonPropertyName("paraGap")] public double ParaGap { get; set; } = 0.25;
    [JsonPropertyName("padL")] public double PadL { get; set; }
    [JsonPropertyName("padR")] public double PadR { get; set; }
    [JsonPropertyName("align")] public string Align { get; set; } = "left";
    [JsonPropertyName("readPct")] public double ReadPct { get; set; }
    [JsonPropertyName("ovType")] public string OvType { get; set; } = "faixa";
    [JsonPropertyName("ovOp")] public double OvOp { get; set; }
    [JsonPropertyName("indType")] public string IndType { get; set; } = "seta";
    [JsonPropertyName("indColor")] public string IndColor { get; set; } = "#ffffff";
    [JsonPropertyName("indLeft")] public double IndLeft { get; set; }
    [JsonPropertyName("indW")] public double IndW { get; set; }
    [JsonPropertyName("indH")] public double IndH { get; set; }
    [JsonPropertyName("readingLine")] public bool ReadingLine { get; set; }
    [JsonPropertyName("pres")] public List<Model.Presenter> Pres { get; set; } = new();
    [JsonPropertyName("presNames")] public bool PresNames { get; set; }
    [JsonPropertyName("presOnly")] public string PresOnly { get; set; } = "0";
    [JsonPropertyName("live")] public bool Live { get; set; }
    [JsonPropertyName("playing")] public bool Playing { get; set; }
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("dir")] public int Dir { get; set; } = 1;
    [JsonPropertyName("speedT")] public double SpeedT { get; set; }
    [JsonPropertyName("speed")] public string Speed { get; set; } = "";
    [JsonPropertyName("block")] public string Block { get; set; } = "";
    [JsonPropertyName("blockLeft")] public string BlockLeft { get; set; } = "";
    [JsonPropertyName("blockLeftWarn")] public bool BlockLeftWarn { get; set; }
    [JsonPropertyName("isLastBlock")] public bool IsLastBlock { get; set; }
    [JsonPropertyName("stamp")] public string Stamp { get; set; } = "";
    [JsonPropertyName("remaining")] public string Remaining { get; set; } = "";
    [JsonPropertyName("clock")] public string Clock { get; set; } = "";
    [JsonPropertyName("webCtrl")] public bool WebCtrl { get; set; }
    [JsonPropertyName("speedLock")] public bool SpeedLock { get; set; }
    [JsonPropertyName("voice")] public string Voice { get; set; } = "";
    [JsonPropertyName("bookmarks")] public List<string> Bookmarks { get; set; } = new();
    [JsonPropertyName("cur")] public int Cur { get; set; } = -1;
    [JsonPropertyName("docName")] public string DocName { get; set; } = "";
}

/// <summary>Comandos aceitos do navegador quando o controle remoto está liberado.</summary>
public static class RemoteCommands
{
    public static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "play", "pause", "togglePlay", "faster", "slower", "reverse", "rewind", "toEnd",
        "nextBm", "prevBm", "nextPara", "prevPara", "lineUp", "lineDown", "speed", "gotoBm", "nudge", "voice"
    };
}
