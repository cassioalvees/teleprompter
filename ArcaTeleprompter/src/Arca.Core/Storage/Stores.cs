using Arca.Core.Model;

namespace Arca.Core.Storage;

/// <summary>Roteiro aberto numa aba.</summary>
public sealed class OpenDoc
{
    public long Id { get; set; }
    public string Name { get; set; } = "Sem título.tps";
    public string? Path { get; set; }
    public List<ScriptBlock> Blocks { get; set; } = new();
    public double Y { get; set; }
    public bool Dirty { get; set; }
}

/// <summary>Sessão (abas abertas), restaurada ao abrir o app. Salva automaticamente.</summary>
public sealed class Session
{
    public List<OpenDoc> Docs { get; set; } = new();
    public long Active { get; set; }
}

/// <summary>Versão guardada do roteiro (histórico).</summary>
public sealed class ScriptVersion
{
    public DateTime Time { get; set; }
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "salva";
    public int Words { get; set; }
    public List<ScriptBlock> Blocks { get; set; } = new();
}

/// <summary>Perfil de apresentador: aparência e velocidade.</summary>
public sealed class Profile
{
    public string Name { get; set; } = "";
    public DateTime Time { get; set; }
    public PrompterSettings Settings { get; set; } = new();
}

/// <summary>Configuração de uma tela de saída.</summary>
public sealed class DisplayConfig
{
    public string Nick { get; set; } = "";
    public bool FlipH { get; set; }
    public bool FlipV { get; set; }
    public string Align { get; set; } = "left";
}

/// <summary>Preferências do app (persistidas em settings.json).</summary>
public sealed class AppSettings
{
    public string Theme { get; set; } = "light";
    public Dictionary<string, string> Keymap { get; set; } = Engine.Keymap.Defaults();
    public bool LockOnPlay { get; set; } = true;
    public bool Countdown { get; set; }
    public bool TalentHud { get; set; } = true;
    /// <summary>Itens das informações na tela (talento e telas do prompter) e posição.</summary>
    public bool HudBlock { get; set; } = true;
    public bool HudTime { get; set; } = true;
    public bool HudClock { get; set; } = true;
    public bool HudBottom { get; set; } = true;
    /// <summary>Roda do mouse/trackpad: linhas por giro com o prompt parado.</summary>
    public double WheelLines { get; set; } = 1.0;
    /// <summary>Roda do mouse/trackpad: fração da faixa de velocidade por giro com o prompt rodando.</summary>
    public double WheelSpeedStep { get; set; } = 0.02;
    /// <summary>Inverte o sentido da roda (rolagem e velocidade).</summary>
    public bool WheelInvert { get; set; }
    /// <summary>Procurar versão nova ao abrir o app (GitHub Releases).</summary>
    public bool UpdateCheck { get; set; } = true;
    /// <summary>Últimas cores escolhidas no seletor de cor (mais recente primeiro).</summary>
    public List<string> RecentColors { get; set; } = new();
    /// <summary>Última versão aberta (para mostrar as novidades depois de uma atualização).</summary>
    public string LastVersion { get; set; } = "";
    /// <summary>Marca da instalação já tratada (data do install.json), para detectar instalação nova/atualização.</summary>
    public long LastInstallStamp { get; set; }
    public bool ShowBookmarks { get; set; } = true;
    public bool ShowInspector { get; set; } = true;
    public int SpeedUnit { get; set; }
    public PrompterSettings Prompter { get; set; } = new();
    public Dictionary<string, DisplayConfig> Displays { get; set; } = new();
    // Monitor web / PWA
    public bool WebOnStart { get; set; }
    public int WebPort { get; set; } = 8420;
    public string WebPassword { get; set; } = "";
    public bool WebControl { get; set; } = true;
    /// <summary>Revisão das preferências (migrações de padrões entre versões).</summary>
    public int SettingsRev { get; set; }
    // NDI
    public string NdiName { get; set; } = "ARCA-PROMPTER (Saída 1)";
    public string NdiFormat { get; set; } = "1080p30";
    public bool NdiMirror { get; set; }
    public bool NdiAlpha { get; set; }
    // Voz
    public string AudioDriver { get; set; } = "wasapi";
    public string AudioDevice { get; set; } = "";
    public int AsioChannel { get; set; }
    public double NoiseGateDb { get; set; } = -58;
    public int VoiceWindow { get; set; } = 20;
    public int VoiceTolerance { get; set; } = 3;
    /// <summary>Antecipação da rolagem por voz, em linhas (0 = linha lida na seta; 1 = uma linha acima).</summary>
    public double VoiceLead { get; set; } = 0.2;
    /// <summary>Volume de entrada da voz em dB (antes do ganho automático).</summary>
    public double VoiceGainDb { get; set; } = 0;
    /// <summary>Ganho automático da voz (AGC).</summary>
    public bool VoiceAgc { get; set; } = true;
    /// <summary>Suavidade da rolagem por voz: tempo (s) para o texto alcançar a palavra ouvida.</summary>
    public double VoiceSmooth { get; set; } = 0.35;
    /// <summary>Exigência: semelhança mínima (0–1) para uma palavra ouvida contar como a do roteiro.</summary>
    public double VoiceStrict { get; set; } = 0.75;
    public string VoiceLanguage { get; set; } = "pt-BR";
    /// <summary>Motor de reconhecimento: vosk (rápido, leve) ou whisper (preciso com microfones reais).</summary>
    public string VoiceEngine { get; set; } = "vosk";
    /// <summary>Modelo do Whisper: base (57 MB) ou small (181 MB).</summary>
    public string WhisperModel { get; set; } = "base";
    /// <summary>Whisper na placa de vídeo (Vulkan) quando houver.</summary>
    public bool WhisperGpu { get; set; } = true;
    // Integrações
    public string MosSystem { get; set; } = "ENPS";
    public string MosHost { get; set; } = "192.168.0.10";
    public string MosId { get; set; } = "ARCA.PROMPTER.1";
    public bool CaptionOut { get; set; }
    /// <summary>tcp = uma linha de texto por mensagem (encoders de legenda, Telnet); vmix = SetText num título do vMix.</summary>
    public string CaptionMode { get; set; } = "vmix";
    public string CaptionHost { get; set; } = "127.0.0.1";
    public int CaptionPort { get; set; } = 8088;
    public string CaptionInput { get; set; } = "Legenda";
    public string CaptionField { get; set; } = "Message.Text";
    public bool MosListen { get; set; }
    public bool GlobalHotkeys { get; set; }
    public bool RecordWithPrompt { get; set; }
}

/// <summary>Acesso aos arquivos de dados.</summary>
public static class Stores
{
    public const int MaxVersions = 25;

    public static AppSettings LoadSettings()
    {
        var s = JsonStore.Load(AppPaths.Settings, () => new AppSettings());
        s.Prompter.MigrateSpeed();
        // 2.3: controle pelo navegador passa a vir ligado (gestos e atalhos do iPad/navegador funcionam de saída).
        if (s.SettingsRev < 3) { s.WebControl = true; s.SettingsRev = 3; }
        return s;
    }
    public static void SaveSettings(AppSettings s) => JsonStore.Save(AppPaths.Settings, s);
    public static Session LoadSession() => JsonStore.Load(AppPaths.Session, () => new Session());
    public static void SaveSession(Session s) => JsonStore.Save(AppPaths.Session, s);
    public static List<ScriptVersion> LoadVersions() => JsonStore.Load(AppPaths.Versions, () => new List<ScriptVersion>());
    public static List<Profile> LoadProfiles() => JsonStore.Load(AppPaths.Profiles, () => new List<Profile>());
    public static void SaveProfiles(List<Profile> p) => JsonStore.Save(AppPaths.Profiles, p);

    public static List<ScriptVersion> AddVersion(List<ScriptVersion> list, string name, ScriptDocument doc, string kind)
    {
        var v = new ScriptVersion { Time = DateTime.Now, Name = name, Kind = kind, Words = doc.WordCount, Blocks = doc.Clone().Blocks };
        var next = new List<ScriptVersion> { v };
        next.AddRange(list.Take(MaxVersions - 1));
        JsonStore.Save(AppPaths.Versions, next);
        return next;
    }
}
