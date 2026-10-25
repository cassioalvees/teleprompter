using System.IO.Compression;

namespace Arca.Audio;

/// <summary>
/// Modelo de voz baixável: Vosk (licença Apache 2.0, zip com uma pasta) ou Whisper (licença MIT, um arquivo ggml .bin
/// multilíngue; <see cref="Folder"/> é o nome do arquivo).
/// </summary>
public sealed record VoiceModel(string Language, string Label, string Folder, string Url, int SizeMb, bool SingleFile = false)
{
    public override string ToString() => Label;
}

/// <summary>Catálogo e download dos modelos de voz offline.</summary>
public static class VoiceModels
{
    public static readonly VoiceModel[] Catalog =
    {
        new("pt-BR", "Português (Brasil/Portugal) · pequeno", "vosk-model-small-pt-0.3", "https://alphacephei.com/vosk/models/vosk-model-small-pt-0.3.zip", 31),
        new("es-ES", "Español · pequeno", "vosk-model-small-es-0.42", "https://alphacephei.com/vosk/models/vosk-model-small-es-0.42.zip", 39),
        new("en-US", "English (US) · pequeno", "vosk-model-small-en-us-0.15", "https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip", 40)
    };

    /// <summary>Whisper multilíngue quantizado (q5_1), do repositório oficial do whisper.cpp. Chave: "base" ou "small".</summary>
    public static readonly VoiceModel[] WhisperCatalog =
    {
        new("base", "Whisper base", "ggml-base-q5_1.bin", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base-q5_1.bin", 57, true),
        new("small", "Whisper small", "ggml-small-q5_1.bin", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small-q5_1.bin", 181, true)
    };

    public static VoiceModel Whisper(string size) =>
        WhisperCatalog.FirstOrDefault(m => m.Language.Equals(size, StringComparison.OrdinalIgnoreCase)) ?? WhisperCatalog[0];

    public static VoiceModel? For(string language) =>
        Catalog.FirstOrDefault(m => m.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
        ?? (language.StartsWith("pt", StringComparison.OrdinalIgnoreCase) ? Catalog[0] : null);

    /// <summary>Baixa (e extrai, se for zip) o modelo em <paramref name="root"/>. Progresso de 0 a 1.</summary>
    public static async Task<string> DownloadAsync(VoiceModel m, string root, IProgress<double>? progress, CancellationToken ct)
    {
        Directory.CreateDirectory(root);
        var zip = Path.Combine(root, m.Folder + (m.SingleFile ? ".part" : ".zip.part"));
        using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) })
        using (var resp = await http.GetAsync(m.Url, HttpCompletionOption.ResponseHeadersRead, ct))
        {
            resp.EnsureSuccessStatusCode();
            var total = resp.Content.Headers.ContentLength ?? m.SizeMb * 1024L * 1024;
            await using var src = await resp.Content.ReadAsStreamAsync(ct);
            await using var dst = File.Create(zip);
            var buf = new byte[1 << 16]; long done = 0; int n;
            while ((n = await src.ReadAsync(buf, ct)) > 0)
            {
                await dst.WriteAsync(buf.AsMemory(0, n), ct);
                done += n; progress?.Report(Math.Min(0.99, (double)done / total));
            }
        }
        if (m.SingleFile)
        {
            // Arquivo único: só renomeia quando o download terminou (um .bin pela metade nunca é encontrado).
            var file = Path.Combine(root, m.Folder);
            File.Move(zip, file, true);
            progress?.Report(1);
            return file;
        }
        var tmp = Path.Combine(root, m.Folder + ".extraindo");
        if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
        ZipFile.ExtractToDirectory(zip, tmp);
        File.Delete(zip);
        // O zip contém a pasta do modelo na raiz.
        var inner = Directory.GetDirectories(tmp).FirstOrDefault() ?? tmp;
        var final = Path.Combine(root, m.Folder);
        if (Directory.Exists(final)) Directory.Delete(final, true);
        Directory.Move(inner, final);
        if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
        progress?.Report(1);
        return final;
    }
}
