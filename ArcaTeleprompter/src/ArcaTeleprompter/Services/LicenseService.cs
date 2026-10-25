using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Arca.Core.Storage;
using Microsoft.Win32;

namespace Arca.App.Services;

/// <summary>Dados da licença ativada (vêm assinados pelo servidor de licenças).</summary>
public sealed record LicenseInfo(string Code, string Name, string Company, string Email, string Kind, DateTime Issued);

/// <summary>
/// Licença do ARCA: um código por pessoa, preso a um computador na ativação online. O servidor devolve a licença
/// assinada (ECDSA P-256); o app confere a assinatura com a chave pública abaixo e o computador — depois disso funciona
/// sem internet (licença vitalícia). Sem licença válida: modo demonstração (<see cref="DemoClock"/>).
/// </summary>
public sealed class LicenseService
{
    /// <summary>Chave pública do servidor de licenças (SPKI, base64). A privada fica só no servidor.</summary>
    private const string PublicKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEKKjxv04mNZv1vyB9ygUhD7Ggo2Oqc+5aQqxpg0kxK5f69GPdPnV595vp+PyYU8GqSMc7qhZnxkkQw6rXmzpTAw==";

    /// <summary>Site e servidor de licenças. <c>ARCA_LICENSE_URL</c> troca o endereço (testes com o servidor local).</summary>
    public const string DefaultServer = "https://arca-licencas.cassiosantos-csa.workers.dev";
    public static string Server => (Environment.GetEnvironmentVariable("ARCA_LICENSE_URL") is { Length: > 0 } u ? u : DefaultServer).TrimEnd('/');

    private static string FilePath => Path.Combine(AppPaths.Data, "licenca.json");

    public LicenseInfo? Info { get; private set; }
    public bool IsLicensed => Info != null;
    public DemoClock Demo { get; }
    /// <summary>Licença mudou (ativada, desativada ou revogada pelo servidor).</summary>
    public event Action? Changed;

    public LicenseService()
    {
        Demo = new DemoClock(MachineId);
        Info = Load();
    }

    // ——— Identificação do computador ———

    private static string? _machine;
    /// <summary>SHA-256 (hex) do identificador do Windows e do número de série do disco do sistema.</summary>
    public static string MachineId => _machine ??= ComputeMachineId();
    /// <summary>Código curto para mostrar ao usuário e ao suporte (ex.: 7F3A-92QD).</summary>
    public static string MachineCode => (MachineId[..4] + "-" + MachineId[4..8]).ToUpperInvariant();

    private static string ComputeMachineId()
    {
        string guid = "";
        try
        {
            using var k = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            guid = k?.GetValue("MachineGuid") as string ?? "";
        }
        catch { }
        uint serial = 0;
        try
        {
            var root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? @"C:\";
            GetVolumeInformation(root, null, 0, out serial, out _, out _, null, 0);
        }
        catch { }
        var raw = $"ARCA|{guid}|{serial:X8}|{(guid.Length == 0 ? Environment.MachineName : "")}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetVolumeInformation(string root, StringBuilder? name, int nameSize, out uint serial, out uint maxComp, out uint flags, StringBuilder? fs, int fsSize);

    // ——— Licença assinada ———

    /// <summary>Confere a licença: assinatura do servidor, produto e computador. Null se não vale aqui.</summary>
    public static LicenseInfo? Verify(string token)
    {
        try
        {
            var dot = token.IndexOf('.'); if (dot <= 0) return null;
            var body = token[..dot]; var sig = FromB64Url(token[(dot + 1)..]);
            using var ec = ECDsa.Create();
            ec.ImportSubjectPublicKeyInfo(Convert.FromBase64String(PublicKey), out _);
            if (!ec.VerifyData(Encoding.ASCII.GetBytes(body), sig, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation)) return null;
            using var doc = JsonDocument.Parse(FromB64Url(body));
            var p = doc.RootElement;
            string S(string k) => p.TryGetProperty(k, out var v) ? v.GetString() ?? "" : "";
            if (S("product") != "ARCA Teleprompter" || S("machine") != MachineId) return null;
            return new LicenseInfo(S("code"), S("name"), S("company"), S("email"), S("kind"), DateTime.TryParse(S("issued"), out var d) ? d : DateTime.MinValue);
        }
        catch { return null; }
    }

    private static byte[] FromB64Url(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }

    private static LicenseInfo? Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(FilePath));
            var info = Verify(doc.RootElement.GetProperty("license").GetString() ?? "");
            if (info == null) Log.Info("Licença: arquivo inválido para este computador — modo demonstração");
            return info;
        }
        catch (Exception e) { Log.Error("Licença", e); return null; }
    }

    private void Save(string token) => File.WriteAllText(FilePath, JsonSerializer.Serialize(new { license = token }));

    private void Clear()
    {
        try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
        Info = null; Changed?.Invoke();
    }

    // ——— Servidor ———

    private static HttpClient Http() => new() { Timeout = TimeSpan.FromSeconds(20), DefaultRequestHeaders = { { "User-Agent", $"ARCA-Teleprompter/{UpdateService.Current}" } } };

    private static async Task<JsonElement> Post(string path, object body)
    {
        using var http = Http();
        using var r = await http.PostAsJsonAsync(Server + path, body);
        var text = await r.Content.ReadAsStringAsync();
        try { return JsonDocument.Parse(text).RootElement.Clone(); }
        catch { throw new HttpRequestException($"Resposta inesperada do servidor de licenças ({(int)r.StatusCode})."); }
    }

    private static string Err(JsonElement r) => r.TryGetProperty("error", out var e) ? e.GetString() ?? "Não foi possível ativar." : "Não foi possível ativar.";

    /// <summary>Ativa o código neste computador. Devolve null se deu certo, ou a mensagem para o usuário.</summary>
    public async Task<string?> Activate(string code)
    {
        try
        {
            var r = await Post("/api/activate", new { code, machine = MachineId, machineName = Environment.MachineName, version = UpdateService.Current.ToString(3) });
            if (!r.TryGetProperty("ok", out var ok) || !ok.GetBoolean()) return Err(r);
            var token = r.GetProperty("license").GetString() ?? "";
            var info = Verify(token);
            if (info == null) return "A licença recebida não confere com este computador. Fale com o suporte.";
            Save(token); Info = info;
            Log.Info($"Licença ativada: {info.Code} · {info.Name} ({info.Company})");
            Changed?.Invoke();
            return null;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            Log.Info("Licença: sem conexão (" + e.Message + ")");
            return "Sem conexão com o servidor de licenças. A ativação precisa de internet — confira a conexão e tente de novo.";
        }
    }

    /// <summary>Libera o código para ativar em outro computador. Devolve null se deu certo, ou a mensagem.</summary>
    public async Task<string?> Deactivate()
    {
        if (Info == null) return null;
        try
        {
            var r = await Post("/api/deactivate", new { code = Info.Code, machine = MachineId });
            if (!r.TryGetProperty("ok", out var ok) || !ok.GetBoolean()) return Err(r);
            Log.Info("Licença desativada neste computador: " + Info.Code);
            Clear();
            return null;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return "Sem conexão com o servidor de licenças. Para desativar e liberar o código, o computador precisa de internet.";
        }
    }

    /// <summary>
    /// Ao abrir o app, em segundo plano: confere se a licença continua valendo (bloqueada pelo administrador, ou ativada
    /// em outro computador). Sem internet não muda nada — a licença é vitalícia. Devolve o motivo se foi revogada.
    /// </summary>
    public async Task<string?> CheckOnline()
    {
        if (Info == null) return null;
        try
        {
            var r = await Post("/api/check", new { code = Info.Code, machine = MachineId, version = UpdateService.Current.ToString(3) });
            if (r.TryGetProperty("valid", out var v) && !v.GetBoolean())
            {
                var reason = r.TryGetProperty("reason", out var why) ? why.GetString() ?? "" : "";
                Log.Info("Licença revogada pelo servidor: " + reason);
                Clear();
                return reason;
            }
        }
        catch (Exception e) { Log.Info("Licença: conferência online indisponível (" + e.Message + ")"); }
        return null;
    }
}

/// <summary>
/// Tempo de prompter da demonstração: 10 minutos por dia, por computador. Guardado com assinatura (HMAC com a
/// identificação do computador) na pasta de dados e no registro do usuário; vale o maior dos dois.
/// </summary>
public sealed class DemoClock
{
    /// <summary>10 minutos por dia. <c>ARCA_DEMO_LIMIT</c> (segundos, testes) só consegue DIMINUIR o limite.</summary>
    public static double DailySeconds { get; } = double.TryParse(Environment.GetEnvironmentVariable("ARCA_DEMO_LIMIT"), out var t) && t > 0 ? Math.Min(600, t) : 600;
    private readonly byte[] _key;
    private string _day = "";
    private double _used;
    private double _unsaved;
    private static string FilePath => Path.Combine(AppPaths.Data, "demo.dat");
    // O registro é compartilhado entre pastas de dados: só vale para a pasta padrão (não nos testes com ARCA_DATA_DIR).
    private static bool UseRegistry => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ARCA_DATA_DIR"));
    private const string RegKey = @"Software\ARCA Teleprompter";

    public DemoClock(string machine) { _key = SHA256.HashData(Encoding.UTF8.GetBytes("demo|" + machine)); Load(); }

    private static string Today => DateTime.Now.ToString("yyyy-MM-dd");
    public double Used { get { Roll(); return _used; } }
    public double Remaining => Math.Max(0, DailySeconds - Used);
    public bool Exhausted => Remaining <= 0;
    public int RemainingMinutes => (int)Math.Ceiling(Remaining / 60);

    private void Roll() { if (_day != Today) { _day = Today; _used = 0; _unsaved = 0; } }

    /// <summary>Soma tempo de prompter rodando (chamado a cada quadro com o prompt ao vivo).</summary>
    public void Add(double seconds)
    {
        Roll();
        _used += seconds; _unsaved += seconds;
        if (_unsaved >= 5 || _used >= DailySeconds) Save();
    }

    public void Save()
    {
        _unsaved = 0;
        var rec = Sign(_day, _used);
        try { File.WriteAllText(FilePath, rec); } catch { }
        if (UseRegistry) try { using var k = Registry.CurrentUser.CreateSubKey(RegKey); k.SetValue("Demo", rec); } catch { }
    }

    private void Load()
    {
        _day = Today; _used = 0;
        foreach (var rec in Records())
            if (Parse(rec) is { } p && p.Day == _day) _used = Math.Max(_used, p.Used);
    }

    private static IEnumerable<string> Records()
    {
        string? a = null, b = null;
        try { if (File.Exists(FilePath)) a = File.ReadAllText(FilePath); } catch { }
        if (UseRegistry) try { using var k = Registry.CurrentUser.OpenSubKey(RegKey); b = k?.GetValue("Demo") as string; } catch { }
        if (a != null) yield return a;
        if (b != null) yield return b;
    }

    private string Sign(string day, double used)
    {
        var body = $"{day}|{used.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}";
        return body + "|" + Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(body)));
    }

    /// <summary>Registro adulterado conta como dia esgotado (apagar o arquivo não devolve o tempo: o registro guarda).</summary>
    private (string Day, double Used)? Parse(string rec)
    {
        var p = rec.Trim().Split('|'); if (p.Length != 3) return null;
        var ok = Sign(p[0], double.TryParse(p[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var u) ? u : 0) == rec.Trim();
        return (p[0], ok ? u : DailySeconds);
    }
}
