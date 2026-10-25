namespace Arca.Core.Engine;

/// <summary>Ação de teclado remapeável (Preferências › Atalhos).</summary>
public sealed record KeyAction(string Id, string Label, string DefaultKey)
{
    public override string ToString() => Label;
}

/// <summary>
/// Atalhos padrão (README §5.7). As teclas usam os nomes de <c>System.Windows.Input.Key</c>
/// ("Space", "Down", "W", "PageDown", "OemCloseBrackets"…), comparados sem diferenciar maiúsculas.
/// </summary>
public static class Keymap
{
    public static readonly KeyAction[] Actions =
    {
        new("play", "Play / pausa", "Space"),
        new("faster", "Mais rápido (pausado: próxima linha)", "Down"),
        new("slower", "Mais devagar (pausado: linha anterior)", "Up"),
        new("faster2", "Mais rápido (alternativo)", "W"),
        new("slower2", "Mais devagar (alternativo)", "S"),
        new("reverse", "Inverter direção", "R"),
        new("stop", "Parar rolagem", "D0"),
        new("rewind", "Voltar ao início", "Home"),
        new("toEnd", "Ir para o fim", "End"),
        new("nextBm", "Próximo marcador", "PageDown"),
        new("prevBm", "Marcador anterior", "PageUp"),
        new("nextPara", "Próximo parágrafo", "OemCloseBrackets"),
        new("prevPara", "Parágrafo anterior", "OemOpenBrackets"),
        new("editHere", "Editar trecho atual", "E"),
        new("mirror", "Espelhar tela do prompter", "M"),
        new("voice", "Ligar / desligar o acompanhamento por voz", "V")
    };

    public static Dictionary<string, string> Defaults() => Actions.ToDictionary(a => a.Id, a => a.DefaultKey);

    /// <summary>Nome amigável da tecla para menus e preferências.</summary>
    public static string Display(string key) => key switch
    {
        "Space" => "Espaço", "Up" => "↑", "Down" => "↓", "Left" => "←", "Right" => "→",
        "PageUp" => "Page Up", "PageDown" => "Page Down", "Escape" => "Esc", "Return" or "Enter" => "Enter",
        "Back" => "Backspace", "OemOpenBrackets" => "[", "OemCloseBrackets" => "]",
        "OemComma" => ",", "OemPeriod" => ".", "OemMinus" => "-", "OemPlus" => "+",
        _ when key.Length == 2 && key[0] == 'D' && char.IsDigit(key[1]) => key[1].ToString(),
        _ when key.StartsWith("NumPad") => "Num " + key[6..],
        _ => key
    };
}
