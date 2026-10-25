// Teste de ponta a ponta do acompanhamento por voz: WAV → Arca.Audio (gate, 16 kHz, Vosk ou Whisper) → Arca.Core (VoiceAligner).
//   VoiceTest [arquivo.wav] [--engine=vosk|whisper] [--grammar] [--quiet] [--models=pasta] [--whisper=base|small|caminho.bin]
//             [--ref=limpo.wav] [--ctx=N] [--window=s]
// --grammar: vocabulário do roteiro (gramática no Vosk, prompt no Whisper), como no app.
// --ref: WAV limpo com a mesma leitura (ex.: longa.wav para fraca/ruim) — dá o instante real de cada palavra
//        (Vosk offline com tempos) e a latência = instante em que a palavra foi seguida − fim da palavra no áudio.
using System.Text.Json;
using Arca.Audio;
using Arca.Core.Engine;
using Arca.Core.Storage;

Console.OutputEncoding = System.Text.Encoding.UTF8;
var inv = System.Globalization.CultureInfo.InvariantCulture;
string? Arg(string name) => args.FirstOrDefault(a => a.StartsWith("--" + name + "="))?[(name.Length + 3)..];
// --degrade=entrada.wav,saida.wav,ganhoDb,ruido : simula microfone fraco com ruído e zumbido de 60 Hz.
if (Arg("degrade") is { } dg)
{
    var p = dg.Split(',');
    using var rd = new NAudio.Wave.AudioFileReader(p[0]);
    var all = new List<float>(); var b = new float[4096]; int n;
    while ((n = rd.Read(b, 0, b.Length)) > 0) all.AddRange(b.Take(n));
    var g = (float)Math.Pow(10, double.Parse(p[2], inv) / 20); var nz = float.Parse(p[3], inv);
    var rnd = new Random(7); var sr0 = rd.WaveFormat.SampleRate;
    // 5º parâmetro opcional: reverberação da sala (RT60 em s) — microfone longe da boca, como num estúdio pequeno.
    if (p.Length > 4 && double.Parse(p[4], inv) is > 0 and var rt60)
    {
        var len = (int)(rt60 * sr0); var ir = new float[len];
        for (var i = 0; i < len; i++) ir[i] = (float)((rnd.NextDouble() * 2 - 1) * Math.Exp(-6.9 * i / len)) * 0.35f;
        ir[0] = 1f;
        var dry = all.ToArray(); var wet = new float[dry.Length];
        for (var i = 0; i < dry.Length; i++) { var x = dry[i]; if (x == 0) continue; for (var k = 0; k < len && i + k < wet.Length; k += 4) wet[i + k] += x * ir[k]; }
        var peak = wet.Max(Math.Abs); all = wet.Select(x => x / Math.Max(1e-6f, peak) * 0.7f).ToList();
    }
    var outS = all.Select((x, i) => x * g + nz * (float)(rnd.NextDouble() * 2 - 1) + nz * 0.8f * (float)Math.Sin(2 * Math.PI * 60 * i / sr0)).ToArray();
    using var wr = new NAudio.Wave.WaveFileWriter(p[1], NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(sr0, rd.WaveFormat.Channels));
    wr.WriteSamples(outS, 0, outS.Length);
    Console.WriteLine($"gravado {p[1]}"); return;
}
var quiet = args.Contains("--quiet");
var roots = AppPaths.VoiceModelDirs().ToList();
if (Arg("models") is { } md) roots.Insert(0, md);
var doc = SampleScripts.JornalDaNoite();
var engine = Arg("engine") ?? "vosk";
var voskModel = SpeechRecognizer.FindModel("pt-BR", roots);
var wav = args.FirstOrDefault(a => !a.StartsWith("--") && a.EndsWith(".wav")) ?? "leitura.wav";

// Referência de tempo: Vosk offline (sem gramática) no WAV limpo, com o instante de cada palavra, alinhado ao roteiro.
var refEnd = new Dictionary<int, double>();
if (Arg("ref") is { } refWav)
{
    if (voskModel == null) throw new Exception("--ref precisa do modelo Vosk pt");
    Vosk.Vosk.SetLogLevel(-1);
    using var vm = new Vosk.Model(voskModel);
    using var rd = new NAudio.Wave.AudioFileReader(refWav);
    var ch = rd.WaveFormat.Channels;
    using var rec = new Vosk.VoskRecognizer(vm, rd.WaveFormat.SampleRate);
    rec.SetWords(true);
    var words = new List<(string W, double End)>();
    void Take(string json)
    {
        using var d = JsonDocument.Parse(json);
        if (d.RootElement.TryGetProperty("result", out var r))
            foreach (var w in r.EnumerateArray()) words.Add((w.GetProperty("word").GetString()!, w.GetProperty("end").GetDouble()));
    }
    var fb = new float[rd.WaveFormat.SampleRate / 10 * ch]; int got;
    while ((got = rd.Read(fb, 0, fb.Length)) > 0)
    {
        var pcm = new short[got / ch];
        for (var i = 0; i < pcm.Length; i++) pcm[i] = (short)Math.Clamp(fb[i * ch] * 32767f, short.MinValue, short.MaxValue);
        if (rec.AcceptWaveform(pcm, pcm.Length)) Take(rec.Result());
    }
    Take(rec.FinalResult());
    var ra = new VoiceAligner(); ra.Load(doc); ra.Start(0);
    foreach (var (w, end) in words) if (ra.Hear(w).Match == 2 && ra.LastMatch is int m) refEnd.TryAdd(m, end);
    Console.WriteLine($"Referência: {words.Count} palavras com tempo, {refEnd.Count} no roteiro");
}

var al = new VoiceAligner(); al.Load(doc); al.Start(0);
var useGrammar = args.Contains("--grammar");
// --pace=N: toca o áudio N vezes mais devagar (simula um computador N vezes mais rápido; tempos medidos no relógio do áudio).
if (Arg("pace") is { } pc) AudioInput.FilePace = double.Parse(pc, inv);
var input = new AudioInput(AudioDriver.File, wav);
IEnumerable<string> Win(int p) => al.Words.Skip(Math.Max(0, p - 40)).Take(260).Select(w => w.Text.ToLowerInvariant());
var vocabAt = 0;
// --preload: prepara o motor antes (como o app faz ao abrir); a partida medida começa depois disso.
if (args.Contains("--preload"))
{
    var pw = System.Diagnostics.Stopwatch.StartNew();
    if (engine == "whisper") { var wm0 = Arg("whisper") ?? "base"; WhisperRecognizer.Preload(File.Exists(wm0) ? wm0 : WhisperRecognizer.FindModel(wm0, roots)!, true); }
    else SpeechRecognizer.Preload(voskModel!);
    Console.WriteLine($"Pré-carga: {pw.Elapsed.TotalSeconds:0.00} s");
}
// Tempo de partida: do pedido para ligar (criar o reconhecedor) até ouvir a primeira palavra.
var startClock = System.Diagnostics.Stopwatch.StartNew();
double firstWordAt = -1, firstWordAudio = -1;
IVoiceRecognizer sr;
var passes = new List<(double Audio, double Ms)>();
if (engine == "whisper")
{
    var wm = Arg("whisper") ?? "base";
    var path = File.Exists(wm) ? wm : WhisperRecognizer.FindModel(wm, roots) ?? throw new Exception("modelo Whisper não encontrado: " + wm);
    Console.WriteLine("Modelo: " + path);
    WhisperRecognizer.FlashAttention = args.Contains("--flash");
    var w = new WhisperRecognizer(input, path, "pt-BR", null) { GateDb = -58 };
    if (Arg("keep") is { } kp) w.Keep = double.Parse(kp, inv);
    if (Arg("lag") is { } lg) w.StableLag = double.Parse(lg, inv);
    if (Arg("prompt") is { } pw) w.PromptWords = int.Parse(pw);
    if (useGrammar) w.SetVocabulary(Win(0));
    if (Arg("ctx") is { } cx) w.AudioContext = int.Parse(cx);
    if (Arg("window") is { } wn) w.MaxWindow = double.Parse(wn, inv);
    w.Pass += (a, ms) => { lock (passes) passes.Add((a, ms)); };
    if (Arg("threads") is { } th) w.Threads = int.Parse(th);
    if (args.Contains("--trace")) w.Trace += t => Console.WriteLine("   " + t);
    if (args.Contains("--noprompt")) w.UsePrompt = false;
    if (args.Contains("--sysinfo")) Console.WriteLine(Whisper.net.WhisperFactory.GetRuntimeInfo() + " · " + Whisper.net.LibraryLoader.RuntimeOptions.LoadedLibrary);
    sr = w;
}
else
{
    var model = voskModel ?? throw new Exception("modelo pt não encontrado");
    Console.WriteLine("Modelo: " + model);
    sr = new SpeechRecognizer(input, model, useGrammar ? Win(0) : null) { GateDb = -58 };
}
Console.WriteLine($"Motor: {engine} · áudio: {wav} · vocabulário do roteiro: {(useGrammar ? "sim" : "não")}");

// Relógio do áudio: amostras entregues pela leitura em tempo real.
long fed = 0;
input.Samples += (_, n) => Interlocked.Add(ref fed, n);
var done = new ManualResetEventSlim();
input.Stopped += _ => { Thread.Sleep((int)(2000 * AudioInput.FilePace)); done.Set(); };
var stats = new int[3];
var lat = new List<double>();
var seen = new HashSet<int>();
sr.Word += w =>
{
    var now = Interlocked.Read(ref fed) / (double)input.SampleRate;
    if (firstWordAt < 0) { firstWordAt = startClock.Elapsed.TotalSeconds; firstWordAudio = now; }
    var h = al.Hear(w); stats[h.Match]++;
    if (h.Match == 2 && al.LastMatch is int m && seen.Add(m) && refEnd.TryGetValue(m, out var e)) lat.Add(now - e);
    if (useGrammar && Math.Abs(al.Position - vocabAt) >= 30) { vocabAt = al.Position; sr.SetVocabulary(Win(vocabAt)); }
    if (!quiet) Console.WriteLine($"{now,6:0.00}s {w,-14} → {al.State,-12} {(al.LastMatch is int mm ? al.Words[mm].Text : "")}");
};
sr.Error += e => Console.WriteLine("ERRO: " + e);
var created = startClock.Elapsed.TotalSeconds;
sr.Start();
Console.WriteLine($"Partida: reconhecedor criado em {created:0.00} s");
done.Wait(TimeSpan.FromSeconds(120 * AudioInput.FilePace));
sr.Dispose();

static double Pct(List<double> v, double p) { if (v.Count == 0) return double.NaN; var s = v.OrderBy(x => x).ToList(); return s[(int)Math.Min(s.Count - 1, Math.Round(p * (s.Count - 1)))]; }
Console.WriteLine($"Partida: primeira palavra {firstWordAt:0.00} s depois de ligar (no áudio: {firstWordAudio:0.00} s; 1ª palavra do roteiro termina em {(refEnd.Count > 0 ? refEnd.Values.Min() : double.NaN):0.00} s)");
Console.WriteLine($"Final: estado {al.State}, posição {al.Position}/{al.Words.Count}");
Console.WriteLine($"Palavras ouvidas: {stats.Sum()} · seguiu {stats[2]} · ressinc. {stats[1]} · fora do roteiro {stats[0]}");
if (refEnd.Count > 0)
    Console.WriteLine($"Cobertura: {seen.Count(refEnd.ContainsKey)}/{refEnd.Count} palavras do roteiro seguidas · latência mediana {Pct(lat, 0.5):0.00} s · p90 {Pct(lat, 0.9):0.00} s");
if (passes.Count > 0)
{
    var ms = passes.Select(p => p.Ms).ToList();
    Console.WriteLine($"Whisper: {passes.Count} transcrições · janela média {passes.Average(p => p.Audio) / 1000:0.0} s · processamento mediano {Pct(ms, 0.5):0} ms · p90 {Pct(ms, 0.9):0} ms");
}
