using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;

namespace Arca.Mos;

/// <summary>Matéria (story) de um espelho (running order).</summary>
public sealed class MosStory
{
    public string Id { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Number { get; set; } = "";
    /// <summary>Parágrafos do texto de leitura (storyBody); vazio até o NCS enviar roStorySend.</summary>
    public List<string> Paragraphs { get; set; } = new();
}

/// <summary>Espelho do jornal recebido da redação (NCS).</summary>
public sealed class RunningOrder
{
    public string Id { get; set; } = "";
    public string Slug { get; set; } = "";
    public List<MosStory> Stories { get; set; } = new();
    public DateTime Updated { get; set; } = DateTime.Now;
}

/// <summary>
/// Dispositivo MOS de prompter (protocolo MOS 2.8.x, subconjunto de running order).
/// Escuta nas portas 10540 (lower) e 10541 (upper); o NCS (ENPS, iNEWS, Octopus, Dalet, Ross Inception)
/// conecta e envia mensagens XML em UTF-16 big-endian. Suportado: heartbeat, reqMachInfo, roCreate, roReplace,
/// roDelete, roStorySend, roElementAction (INSERT/REPLACE/MOVE/DELETE de stories), roReadyToAir, roMetadataReplace.
/// Cada alteração gera <see cref="RunningOrderChanged"/> com o espelho completo.
/// </summary>
public sealed class MosDevice : IDisposable
{
    private readonly List<TcpListener> _listeners = new();
    private readonly Dictionary<string, RunningOrder> _ros = new();
    private CancellationTokenSource? _cts;
    private int _clients;

    public string MosId { get; set; } = "ARCA.PROMPTER.1";
    public string NcsId { get; set; } = "NCS";
    public int LowerPort { get; set; } = 10540;
    public int UpperPort { get; set; } = 10541;
    public bool Running => _cts != null;
    public DateTime? LastContact { get; private set; }
    public string? LastPeer { get; private set; }
    public int Connections => _clients;

    public event Action<RunningOrder>? RunningOrderChanged;
    public event Action<string>? RunningOrderDeleted;
    public event Action<string>? Activity;

    public IReadOnlyCollection<RunningOrder> RunningOrders => _ros.Values;

    public void Start()
    {
        if (_cts != null) return;
        _cts = new CancellationTokenSource();
        foreach (var port in new[] { LowerPort, UpperPort })
        {
            var l = new TcpListener(IPAddress.Any, port);
            l.Start();
            _listeners.Add(l);
            _ = AcceptLoop(l, _cts.Token);
        }
    }

    public void Stop()
    {
        _cts?.Cancel(); _cts = null;
        foreach (var l in _listeners) { try { l.Stop(); } catch { /* */ } }
        _listeners.Clear();
    }

    public void Dispose() => Stop();

    private async Task AcceptLoop(TcpListener l, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient c;
            try { c = await l.AcceptTcpClientAsync(ct); } catch { return; }
            _ = Handle(c, ct);
        }
    }

    private async Task Handle(TcpClient c, CancellationToken ct)
    {
        Interlocked.Increment(ref _clients);
        LastPeer = (c.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString();
        Activity?.Invoke($"Conexão do NCS {LastPeer}");
        using (c)
        {
            var s = c.GetStream();
            var buf = new byte[64 * 1024];
            var pending = new StringBuilder();
            Encoding? enc = null;
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var n = await s.ReadAsync(buf, ct);
                    if (n == 0) break;
                    enc ??= Detect(buf, n);
                    pending.Append(enc.GetString(buf, 0, n));
                    foreach (var msg in Split(pending))
                    {
                        var reply = Process(msg);
                        if (reply != null)
                        {
                            var bytes = enc.GetBytes(reply);
                            await s.WriteAsync(bytes, ct);
                        }
                    }
                }
            }
            catch (Exception e) when (e is IOException or OperationCanceledException or SocketException) { /* desconectou */ }
        }
        Interlocked.Decrement(ref _clients);
    }

    /// <summary>MOS usa UTF-16 big-endian; aceita também UTF-8 (alguns simuladores).</summary>
    private static Encoding Detect(byte[] b, int n) => n >= 2 && b[0] == 0 && b[1] == (byte)'<' ? Encoding.BigEndianUnicode : n >= 2 && b[0] == (byte)'<' && b[1] == 0 ? Encoding.Unicode : Encoding.UTF8;

    /// <summary>Extrai mensagens &lt;mos&gt;…&lt;/mos&gt; completas do buffer.</summary>
    private static IEnumerable<string> Split(StringBuilder sb)
    {
        var list = new List<string>();
        while (true)
        {
            var t = sb.ToString();
            var a = t.IndexOf("<mos>", StringComparison.Ordinal);
            if (a < 0) a = t.IndexOf("<mos ", StringComparison.Ordinal);
            var b = t.IndexOf("</mos>", StringComparison.Ordinal);
            if (a < 0 || b < 0) break;
            list.Add(t.Substring(a, b + 6 - a));
            sb.Remove(0, b + 6);
        }
        return list;
    }

    /// <summary>Processa uma mensagem e devolve a resposta (ou null).</summary>
    public string? Process(string xml)
    {
        XElement m;
        try { m = XElement.Parse(xml); } catch (System.Xml.XmlException) { return null; }
        LastContact = DateTime.Now;
        NcsId = (string?)m.Element("ncsID") ?? NcsId;
        var body = m.Elements().FirstOrDefault(e => e.Name.LocalName is not ("mosID" or "ncsID" or "messageID"));
        if (body == null) return null;
        var kind = body.Name.LocalName;
        Activity?.Invoke(kind);
        switch (kind)
        {
            case "heartbeat":
                return Wrap(new XElement("heartbeat", new XElement("time", DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss"))));
            case "reqMachInfo":
                return Wrap(new XElement("listMachInfo",
                    new XElement("manufacturer", "ARCA"), new XElement("model", "ARCA Teleprompter"), new XElement("hwRev"), new XElement("swRev", "2.0"),
                    new XElement("DOM"), new XElement("SN", Environment.MachineName), new XElement("ID", MosId), new XElement("time", DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss")),
                    new XElement("mosRev", "2.8.5"),
                    new XElement("supportedProfiles", new XAttribute("deviceType", "MOS"), new XElement("mosProfile", new XAttribute("number", "0"), "YES"), new XElement("mosProfile", new XAttribute("number", "2"), "YES"))));
            case "roCreate":
            case "roReplace":
            {
                var ro = new RunningOrder { Id = (string?)body.Element("roID") ?? "", Slug = (string?)body.Element("roSlug") ?? "Espelho" };
                foreach (var st in body.Elements("story")) ro.Stories.Add(ParseStory(st));
                // roReplace preserva os textos já recebidos das matérias que continuam.
                if (_ros.TryGetValue(ro.Id, out var old))
                    foreach (var s in ro.Stories) if (s.Paragraphs.Count == 0 && old.Stories.FirstOrDefault(o => o.Id == s.Id) is { } os) s.Paragraphs = os.Paragraphs;
                _ros[ro.Id] = ro; Changed(ro);
                return Ack(ro.Id);
            }
            case "roDelete":
            {
                var id = (string?)body.Element("roID") ?? "";
                _ros.Remove(id); RunningOrderDeleted?.Invoke(id);
                return Ack(id);
            }
            case "roStorySend":
            {
                var roId = (string?)body.Element("roID") ?? "";
                if (!_ros.TryGetValue(roId, out var ro)) { ro = new RunningOrder { Id = roId, Slug = "Espelho " + roId }; _ros[roId] = ro; }
                var sid = (string?)body.Element("storyID") ?? "";
                var story = ro.Stories.FirstOrDefault(s => s.Id == sid);
                if (story == null) { story = new MosStory { Id = sid }; ro.Stories.Add(story); }
                story.Slug = (string?)body.Element("storySlug") ?? story.Slug;
                story.Number = (string?)body.Element("storyNum") ?? story.Number;
                story.Paragraphs = ParseBody(body.Element("storyBody"));
                Changed(ro);
                return Ack(roId, sid);
            }
            case "roElementAction":
            {
                var op = ((string?)body.Attribute("operation") ?? "").ToUpperInvariant();
                var roId = (string?)body.Element("roID") ?? "";
                if (!_ros.TryGetValue(roId, out var ro)) return Ack(roId, status: "Unknown roID");
                var target = body.Element("element_target");
                var targetId = (string?)target?.Element("storyID");
                var src = body.Element("element_source");
                var at = targetId == null ? ro.Stories.Count : ro.Stories.FindIndex(s => s.Id == targetId);
                if (at < 0) at = ro.Stories.Count;
                switch (op)
                {
                    case "INSERT":
                        ro.Stories.InsertRange(at, src?.Elements("story").Select(ParseStory) ?? Enumerable.Empty<MosStory>());
                        break;
                    case "REPLACE":
                        if (at < ro.Stories.Count)
                        {
                            var repl = src?.Elements("story").Select(ParseStory).ToList() ?? new();
                            var keep = ro.Stories[at];
                            foreach (var r in repl) if (r.Id == keep.Id && r.Paragraphs.Count == 0) r.Paragraphs = keep.Paragraphs;
                            ro.Stories.RemoveAt(at); ro.Stories.InsertRange(at, repl);
                        }
                        break;
                    case "MOVE":
                    {
                        var ids = src?.Elements("storyID").Select(e => e.Value).ToList() ?? new();
                        var moving = ro.Stories.Where(s => ids.Contains(s.Id)).ToList();
                        ro.Stories.RemoveAll(s => ids.Contains(s.Id));
                        var idx = targetId == null ? ro.Stories.Count : ro.Stories.FindIndex(s => s.Id == targetId);
                        ro.Stories.InsertRange(idx < 0 ? ro.Stories.Count : idx, moving);
                        break;
                    }
                    case "DELETE":
                    {
                        var ids = src?.Elements("storyID").Select(e => e.Value).ToHashSet() ?? new();
                        ro.Stories.RemoveAll(s => ids.Contains(s.Id));
                        break;
                    }
                    case "SWAP":
                    {
                        var ids = src?.Elements("storyID").Select(e => e.Value).ToList() ?? new();
                        if (ids.Count == 2)
                        {
                            int a = ro.Stories.FindIndex(s => s.Id == ids[0]), b = ro.Stories.FindIndex(s => s.Id == ids[1]);
                            if (a >= 0 && b >= 0) (ro.Stories[a], ro.Stories[b]) = (ro.Stories[b], ro.Stories[a]);
                        }
                        break;
                    }
                }
                Changed(ro);
                return Ack(roId);
            }
            case "roMetadataReplace":
            {
                var roId = (string?)body.Element("roID") ?? "";
                if (_ros.TryGetValue(roId, out var ro)) { ro.Slug = (string?)body.Element("roSlug") ?? ro.Slug; Changed(ro); }
                return Ack(roId);
            }
            case "roReq":
            case "roReadyToAir":
            case "roStoryMoveMultiple":
            default:
                return Ack((string?)body.Element("roID") ?? "");
        }
    }

    private void Changed(RunningOrder ro) { ro.Updated = DateTime.Now; RunningOrderChanged?.Invoke(ro); }

    private static MosStory ParseStory(XElement st) => new()
    {
        Id = (string?)st.Element("storyID") ?? "",
        Slug = (string?)st.Element("storySlug") ?? "",
        Number = (string?)st.Element("storyNum") ?? "",
        Paragraphs = ParseBody(st.Element("storyBody"))
    };

    /// <summary>storyBody: parágrafos &lt;p&gt;; instruções entre colchetes ou &lt;pi&gt; (prompter instruction) viram texto entre colchetes.</summary>
    public static List<string> ParseBody(XElement? body)
    {
        var list = new List<string>();
        if (body == null) return list;
        foreach (var p in body.Descendants().Where(e => e.Name.LocalName is "p" or "pi"))
        {
            var t = string.Concat(p.Nodes().Select(n => n is XText x ? x.Value : n is XElement e && e.Name.LocalName == "pi" ? "[" + e.Value + "]" : n is XElement e2 && e2.Name.LocalName != "storyItem" ? e2.Value : "")).Trim();
            if (p.Name.LocalName == "pi" && p.Parent?.Name.LocalName == "p") continue;
            if (p.Name.LocalName == "pi") t = "[" + p.Value.Trim() + "]";
            if (t.Length > 0) list.Add(System.Text.RegularExpressions.Regex.Replace(t, @"\s+", " "));
        }
        return list;
    }

    private string Wrap(XElement body) => new XElement("mos", new XElement("mosID", MosId), new XElement("ncsID", NcsId), new XElement("messageID", Environment.TickCount & 0x7fffffff), body).ToString(SaveOptions.DisableFormatting);
    private string Ack(string roId, string? storyId = null, string status = "OK")
    {
        var ack = new XElement("roAck", new XElement("roID", roId), new XElement("roStatus", status));
        if (storyId != null) { ack.Add(new XElement("storyID", storyId)); ack.Add(new XElement("status", "OK")); }
        return Wrap(ack);
    }
}
