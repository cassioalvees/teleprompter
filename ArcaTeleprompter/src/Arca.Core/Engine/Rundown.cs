using Arca.Core.Model;

namespace Arca.Core.Engine;

/// <summary>Tempos do rundown (README §5.3).</summary>
public static class Rundown
{
    /// <summary>Palavras por segundo na velocidade atual (mínimo 0,1).</summary>
    public static double WordsPerSecond(PrompterSettings s) => Math.Max(0.1, s.LinesPerSecond * s.WordsPerLine);

    /// <summary>Tempo estimado do bloco: palavras ÷ (linhas/s × palavras por linha).</summary>
    public static double BlockSeconds(RundownBlock b, PrompterSettings s) => b.Words / WordsPerSecond(s);

    /// <summary>Soma só os blocos que vão ao ar.</summary>
    public static double TotalSeconds(IEnumerable<RundownBlock> blocks, PrompterSettings s) => blocks.Where(b => !b.Skip).Sum(b => BlockSeconds(b, s));

    /// <summary>Diferença para a duração do programa: positivo = estouro.</summary>
    public static double Over(IEnumerable<RundownBlock> blocks, PrompterSettings s) => TotalSeconds(blocks, s) - s.ProgramSeconds;
}

/// <summary>Roteiro de exemplo aberto na primeira execução.</summary>
public static class SampleScripts
{
    public static ScriptDocument JornalDaNoite()
    {
        var d = new ScriptDocument();
        void P(params TextRun[] r) => d.Blocks.Add(new ScriptBlock { Runs = r.ToList() });
        d.Blocks.Add(ScriptBlock.Mark("Abertura"));
        P(new TextRun("Boa noite. Começa agora o "), new TextRun("Jornal da Noite", Bold: true), new TextRun(". Nesta edição, as chuvas que atingem o litoral, os novos números da economia e a decisão do campeonato."));
        d.Blocks.Add(ScriptBlock.Mark("Chuvas no litoral"));
        P(new TextRun("A chuva forte das últimas vinte e quatro horas deixou cidades do litoral em estado de alerta."));
        P(new TextRun("A Defesa Civil registrou mais de cento e vinte milímetros em algumas regiões, e moradores de áreas de risco foram orientados a deixar suas casas."));
        d.Blocks.Add(ScriptBlock.Pause("Entra VT · 1 min 45 s"));
        P(new TextRun("A previsão é de que o tempo melhore a partir de quinta-feira."));
        d.Blocks.Add(ScriptBlock.Mark("Economia"));
        P(new TextRun("Na economia, a inflação de setembro ficou "), new TextRun("abaixo do esperado", Color: "#ffd54a"), new TextRun(" pelo mercado."));
        P(new TextRun("O resultado foi puxado principalmente pela queda no preço dos alimentos e dos combustíveis. Com isso, analistas já falam em novos cortes na taxa de juros até o fim do ano."));
        d.Blocks.Add(ScriptBlock.Mark("Esporte"));
        P(new TextRun("No esporte, a rodada decisiva do campeonato reservou emoção até o último minuto. Com um gol nos acréscimos, o líder garantiu a vantagem para a reta final."));
        d.Blocks.Add(ScriptBlock.Mark("Encerramento"));
        P(new TextRun("O Jornal da Noite fica por aqui. "), new TextRun("Obrigado pela companhia", Underline: true), new TextRun(" e até amanhã."));
        return d;
    }
}
