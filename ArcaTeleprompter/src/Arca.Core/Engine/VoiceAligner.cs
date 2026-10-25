using Arca.Core.Model;

namespace Arca.Core.Engine;

/// <summary>Estados do acompanhamento por voz.</summary>
public enum VoiceState { Off, Listening, Following, Improvising, Resyncing }

/// <summary>Uma palavra do roteiro, com a localização no documento (bloco e posição no texto do bloco).</summary>
public sealed record ScriptWord(int Block, int CharIndex, int Length, string Text, string Norm);

/// <summary>Resultado de uma palavra ouvida.</summary>
public sealed record HeardWord(string Word, int Match /* 2 = seguiu, 1 = ressinc., 0 = fora do roteiro */);

/// <summary>
/// Alinha as palavras reconhecidas (ASR) com o roteiro, como o VoiceTracking do Autoscript:
/// procura cada palavra numa janela à frente da posição atual (Levenshtein ≥ 0,75); após
/// <see cref="Tolerance"/> erros seguidos entra em IMPROVISO (rolagem para) e passa a procurar numa janela
/// maior, para trás e para frente. Volta a SEGUIR quando encontra duas palavras consecutivas.
/// </summary>
public sealed class VoiceAligner
{
    private readonly List<ScriptWord> _words = new();
    private int _misses;
    private int? _pending;
    private int? _jump;

    /// <summary>Palavras que podem ser puladas sem confirmação (o ASR costuma perder palavras curtas).</summary>
    public int MaxSkip { get; set; } = 3;

    /// <summary>Quantas palavras à frente procurar enquanto segue.</summary>
    public int Window { get; set; } = 20;
    /// <summary>Erros seguidos até considerar improviso.</summary>
    public int Tolerance { get; set; } = 3;
    public double Threshold { get; set; } = 0.75;

    public IReadOnlyList<ScriptWord> Words => _words;
    /// <summary>Índice da próxima palavra esperada.</summary>
    public int Position { get; private set; }
    public VoiceState State { get; private set; } = VoiceState.Off;
    /// <summary>Índice da última palavra reconhecida no roteiro (para posicionar a rolagem).</summary>
    public int? LastMatch { get; private set; }

    /// <summary>Monta a lista de palavras a partir dos blocos de texto não pulados.</summary>
    public void Load(ScriptDocument doc)
    {
        _words.Clear();
        var skipped = doc.SkippedMask();
        for (var i = 0; i < doc.Blocks.Count; i++)
        {
            var b = doc.Blocks[i];
            if (b.Kind != BlockKind.Text || skipped[i]) continue;
            foreach (var (idx, w) in TextUtil.Words(b.PlainText)) _words.Add(new ScriptWord(i, idx, w.Length, w, TextUtil.Normalize(w)));
        }
        Position = Math.Min(Position, _words.Count);
    }

    public void Start(int fromWord)
    {
        Position = Math.Clamp(fromWord, 0, _words.Count);
        _misses = 0; _pending = null; _jump = null; LastMatch = null;
        State = VoiceState.Listening;
    }

    public void Stop() { State = VoiceState.Off; _pending = null; }

    /// <summary>Processa uma palavra ouvida. Retorna o resultado para exibir na lista "ouvidas".</summary>
    public HeardWord Hear(string raw)
    {
        var n = TextUtil.Normalize(raw);
        if (n.Length == 0 || State == VoiceState.Off) return new HeardWord(raw, 0);
        bool Like(int i) => TextUtil.Similarity(n, _words[i].Norm) >= Threshold;
        // Palavras de uma letra ("e", "o", "a") não ancoram nem contam como erro.
        if (n.Length < 2) return new HeardWord(raw, 1);

        if (_misses >= Tolerance)
        {
            // IMPROVISO: só volta a seguir com duas palavras consecutivas do roteiro.
            // Aceita a palavra seguinte à âncora ou a próxima depois dela (o ASR perde palavras curtas).
            if (_pending is int p)
                for (var k = 1; k <= 2 && p + k < _words.Count; k++)
                    if (Like(p + k))
                    {
                        Position = p + k + 1; LastMatch = p + k; _misses = 0; _pending = null; State = VoiceState.Following;
                        return new HeardWord(raw, 2);
                    }
            // Palavras muito curtas ("e", "o", "a") geram falsos positivos: não ancoram, mas não desfazem a âncora.
            if (n.Length < 3) return new HeardWord(raw, 0);
            // Procura para trás (repetiu) e para frente (pulou), preferindo o trecho mais perto da posição.
            int from = Math.Max(0, Position - 10), to = Math.Min(_words.Count, Position + 30), best = -1;
            for (var i = from; i < to; i++)
                if (Like(i) && (best < 0 || Math.Abs(i - Position) < Math.Abs(best - Position))) best = i;
            if (best >= 0) { _pending = best; State = VoiceState.Resyncing; return new HeardWord(raw, 1); }
            _pending = null; State = VoiceState.Improvising;
            return new HeardWord(raw, 0);
        }

        // Confirmação de um salto proposto pela palavra anterior.
        if (_jump is int j)
        {
            _jump = null;
            if (j + 1 < _words.Count && Like(j + 1))
            {
                Position = j + 2; LastMatch = j + 1; _misses = 0; State = VoiceState.Following;
                return new HeardWord(raw, 2);
            }
        }
        var hit = -1;
        if (n.Length >= 2)
            for (int i = Position, end = Math.Min(_words.Count, Position + Window); i < end; i++) if (Like(i)) { hit = i; break; }
        // Até MaxSkip palavras à frente (o ASR perde artigos e preposições): segue direto.
        if (hit >= 0 && hit <= Position + MaxSkip)
        {
            Position = hit + 1; LastMatch = hit; _misses = 0; State = VoiceState.Following;
            return new HeardWord(raw, 2);
        }
        // Mais longe: só aceita se a próxima palavra confirmar (evita saltos por palavras parecidas).
        if (hit >= 0) _jump = hit;
        _misses++;
        if (_misses >= Tolerance) { State = VoiceState.Improvising; _pending = null; }
        return new HeardWord(raw, 0);
    }

    /// <summary>Primeira palavra no bloco indicado ou depois dele.</summary>
    public int WordAtOrAfterBlock(int block)
    {
        for (var i = 0; i < _words.Count; i++) if (_words[i].Block >= block) return i;
        return _words.Count;
    }
}
