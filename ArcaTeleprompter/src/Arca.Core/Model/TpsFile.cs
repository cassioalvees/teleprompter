using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Arca.Core.Model;

/// <summary>
/// Arquivo de roteiro .tps (JSON).
/// v1 (protótipo/app web): { app, v:1, html, settings }.
/// v2 (este app): { app, v:2, blocks, html, settings } — o campo html continua sendo gravado para
/// que versões anteriores consigam abrir o arquivo.
/// </summary>
public static class TpsFile
{
    public const string AppName = "ARCA Teleprompter";

    public static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public sealed record Loaded(ScriptDocument Document, PrompterSettings? Settings);

    public static Loaded Parse(string json)
    {
        var node = JsonNode.Parse(json) as JsonObject ?? throw new InvalidDataException("Arquivo .tps inválido.");
        ScriptDocument doc;
        if (node["blocks"] is JsonArray arr)
            doc = new ScriptDocument { Blocks = arr.Deserialize<List<ScriptBlock>>(Json) ?? new() };
        else if (node["html"]?.GetValue<string>() is { } html)
            doc = HtmlScript.Parse(html);
        else throw new InvalidDataException("O arquivo não contém um roteiro.");
        PrompterSettings? settings = null;
        if (node["settings"] is JsonObject s)
        {
            try { settings = s.Deserialize<PrompterSettings>(new JsonSerializerOptions { NumberHandling = JsonNumberHandling.AllowReadingFromString })?.MigrateSpeed(); }
            catch (JsonException) { /* configurações incompatíveis: ignora */ }
        }
        if (doc.Blocks.Count == 0) doc.Blocks.Add(ScriptBlock.Paragraph(""));
        return new Loaded(doc, settings);
    }

    public static string Serialize(ScriptDocument doc, PrompterSettings settings)
    {
        var o = new JsonObject
        {
            ["app"] = AppName,
            ["v"] = 2,
            ["blocks"] = JsonSerializer.SerializeToNode(doc.Blocks, Json),
            ["html"] = HtmlScript.ToHtml(doc),
            ["settings"] = JsonSerializer.SerializeToNode(settings)
        };
        return o.ToJsonString();
    }
}
