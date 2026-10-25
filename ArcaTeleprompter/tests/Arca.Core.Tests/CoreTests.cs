using Arca.Core.Engine;
using Arca.Core.Model;

namespace Arca.Core.Tests;

public class HtmlScriptTests
{
    [Fact]
    public void Parses_prototype_html_with_bookmarks_stops_presenters_and_format()
    {
        var html = "<p data-bm=\"1\" contenteditable=\"false\">Abertura</p><p data-who=\"2\">Boa <b>noite</b> &amp; <span style=\"color:#ffd54a\">olá</span></p><p data-stop=\"1\">Entra VT</p><p data-bm=\"1\" data-skip=\"1\">Pulado</p><p>x</p>";
        var d = HtmlScript.Parse(html);
        Assert.Equal(5, d.Blocks.Count);
        Assert.Equal(BlockKind.Bookmark, d.Blocks[0].Kind);
        Assert.Equal("Abertura", d.Blocks[0].PlainText);
        Assert.Equal(2, d.Blocks[1].Who);
        Assert.Equal("Boa noite & olá", d.Blocks[1].PlainText);
        Assert.Contains(d.Blocks[1].Runs, r => r.Bold && r.Text == "noite");
        Assert.Contains(d.Blocks[1].Runs, r => r.Color == "#ffd54a");
        Assert.Equal(BlockKind.Stop, d.Blocks[2].Kind);
        Assert.True(d.Blocks[3].Skip);
        Assert.Equal(new[] { false, false, false, false, true }, d.SkippedMask());
    }

    [Fact]
    public void Html_round_trip_keeps_structure()
    {
        var d = SampleScripts.JornalDaNoite();
        var back = HtmlScript.Parse(HtmlScript.ToHtml(d));
        Assert.Equal(d.Blocks.Count, back.Blocks.Count);
        Assert.Equal(d.WordCount, back.WordCount);
        Assert.Equal(d.Blocks.Select(b => b.Kind), back.Blocks.Select(b => b.Kind));
    }

    [Fact]
    public void Tps_v2_round_trip_and_v1_compatibility()
    {
        var d = SampleScripts.JornalDaNoite();
        var json = TpsFile.Serialize(d, new PrompterSettings { WordsPerLine = 7 });
        var l = TpsFile.Parse(json);
        Assert.Equal(d.Blocks.Count, l.Document.Blocks.Count);
        Assert.Equal(7, l.Settings!.WordsPerLine);
        var v1 = "{\"app\":\"ARCA Prompter\",\"v\":1,\"html\":\"<p data-bm=\\\"1\\\">A</p><p>texto</p>\",\"settings\":{\"wpl\":6,\"bg\":\"#111111\"}}";
        var l1 = TpsFile.Parse(v1);
        Assert.Equal(2, l1.Document.Blocks.Count);
        Assert.Equal(6, l1.Settings!.WordsPerLine);
        Assert.Equal("#111111", l1.Settings.Background);
    }

    [Fact]
    public void Plain_text_import_conventions()
    {
        var d = ScriptDocument.FromPlainText("*Bloco 1\nOlá mundo\n[PAUSA] VT\n\n[pausa]\n");
        Assert.Equal(new[] { BlockKind.Bookmark, BlockKind.Text, BlockKind.Stop, BlockKind.Stop }, d.Blocks.Select(b => b.Kind));
        Assert.Equal("Pausa", d.Blocks[3].PlainText);
    }
}

public class RundownTests
{
    [Fact]
    public void Move_block_moves_bookmark_and_content()
    {
        var d = new ScriptDocument();
        d.Blocks.Add(ScriptBlock.Mark("A")); d.Blocks.Add(ScriptBlock.Paragraph("a1"));
        d.Blocks.Add(ScriptBlock.Mark("B")); d.Blocks.Add(ScriptBlock.Paragraph("b1")); d.Blocks.Add(ScriptBlock.Paragraph("b2"));
        d.Blocks.Add(ScriptBlock.Mark("C")); d.Blocks.Add(ScriptBlock.Paragraph("c1"));
        Assert.True(d.MoveRundownBlock(2, 0));
        Assert.Equal("C c1 A a1 B b1 b2", string.Join(' ', d.Blocks.Select(b => b.PlainText)));
        Assert.True(d.MoveRundownBlock(0, 3));
        Assert.Equal("A a1 B b1 b2 C c1", string.Join(' ', d.Blocks.Select(b => b.PlainText)));
        Assert.False(d.MoveRundownBlock(1, 2));
    }

    [Fact]
    public void Skipped_blocks_do_not_count_in_total()
    {
        var d = new ScriptDocument();
        d.Blocks.Add(ScriptBlock.Mark("A")); d.Blocks.Add(ScriptBlock.Paragraph("um dois três quatro"));
        d.Blocks.Add(ScriptBlock.Mark("B", skip: true)); d.Blocks.Add(ScriptBlock.Paragraph("cinco seis"));
        var s = new PrompterSettings { SpeedT = 0, MinSpeed = 1, WordsPerLine = 2 }; // 2 palavras/s
        Assert.Equal(2, Rundown.TotalSeconds(d.Rundown(), s), 3);
    }

    [Fact]
    public void Who_start_marks_first_paragraph_of_each_speaker_run()
    {
        var d = new ScriptDocument();
        d.Blocks.Add(ScriptBlock.Paragraph("a", 1)); d.Blocks.Add(ScriptBlock.Paragraph("b", 1));
        d.Blocks.Add(ScriptBlock.Paragraph("c", 2)); d.Blocks.Add(ScriptBlock.Pause("p")); d.Blocks.Add(ScriptBlock.Paragraph("d", 2));
        Assert.Equal(new[] { true, false, true, false, true }, d.WhoStartMask());
    }

    [Fact]
    public void Remove_presenter_renumbers()
    {
        var d = new ScriptDocument();
        d.Blocks.Add(ScriptBlock.Paragraph("a", 1)); d.Blocks.Add(ScriptBlock.Paragraph("b", 2)); d.Blocks.Add(ScriptBlock.Paragraph("c", 3));
        d.RemovePresenter(2);
        Assert.Equal(new[] { 1, 0, 2 }, d.Blocks.Select(b => b.Who));
    }
}

public class EngineTests
{
    [Fact]
    public void Layout_keeps_text_clear_of_indicator_and_respects_margin_position()
    {
        var s = new PrompterSettings { WordsPerLine = 5, Margin = 200, MarginPos = -1 };
        var l = PrompterLayout.Compute(s, 1);
        Assert.True(l.PaddingLeft > 24);
        Assert.Equal(224, l.PaddingRight);
        var c = PrompterLayout.Compute(new PrompterSettings { Margin = 200, MarginPos = 0 }, 1);
        Assert.Equal(124, c.PaddingRight);
        Assert.True(l.FontSize > 30 && l.FontSize < 120);
    }

    [Fact]
    public void Scroll_stops_at_pause_and_at_end()
    {
        var s = new PrompterSettings { SpeedT = 1, MaxSpeed = 3, MinSpeed = 0.3 };
        var e = new ScrollEngine(s) { LineHeight = 50, EndY = 1000, Stops = new[] { 300.0 } };
        e.Play();
        for (var i = 0; i < 200 && e.Playing; i++) e.Tick(1 / 60.0);
        Assert.False(e.Playing);
        Assert.Equal(290, e.Y, 1);
        e.Play();
        for (var i = 0; i < 1000 && e.Playing; i++) e.Tick(1 / 60.0);
        Assert.Equal(1000, e.Y, 3);
    }

    [Fact]
    public void Slower_stops_at_minimum_and_never_reverses()
    {
        var s = new PrompterSettings { SpeedT = 0.03 };
        var e = new ScrollEngine(s);
        e.AdjustSpeed(-0.05);
        Assert.Equal(1, e.Direction);
        Assert.Equal(0, s.SpeedT, 3);
        e.AdjustSpeed(-0.05);
        Assert.Equal(0, s.SpeedT, 3);
        e.Reverse();
        Assert.Equal(-1, e.Direction);
    }

    [Fact]
    public void Speed_change_has_constant_acceleration()
    {
        var s = new PrompterSettings { SpeedT = 1 };
        var e = new ScrollEngine(s) { LineHeight = 100, EndY = 1e9 };
        e.Play();
        for (var i = 0; i < 120; i++) e.Tick(1 / 60.0);       // estável na velocidade máxima
        e.AdjustSpeed(-1);                                    // freia de uma vez
        var vs = new List<double>();
        for (var i = 0; i < 30; i++) { e.Tick(1 / 60.0); vs.Add(e.Velocity); }
        var steps = vs.Zip(vs.Skip(1), (a, b) => a - b).Where(d => d > 1e-6).ToList();
        Assert.True(steps.Max() - steps.Min() < 1e-6 * 1000, "a redução por quadro deve ser constante");
        Assert.Equal(s.LinesPerSecond * 100, vs[^1], 3);       // chegou ao alvo em ≤ 0,5 s
    }

    [Fact]
    public void Voice_follows_detects_improvisation_and_resyncs()
    {
        var d = ScriptDocument.FromPlainText("Boa noite começa agora o jornal da noite com as notícias do dia");
        var v = new VoiceAligner { Tolerance = 3 };
        v.Load(d); v.Start(0);
        foreach (var w in "boa noite começa".Split(' ')) Assert.Equal(2, v.Hear(w).Match);
        Assert.Equal(VoiceState.Following, v.State);
        foreach (var w in "deixa eu fazer".Split(' ')) v.Hear(w);
        Assert.Equal(VoiceState.Improvising, v.State);
        v.Hear("jornal");
        Assert.Equal(VoiceState.Resyncing, v.State);
        v.Hear("da");
        v.Hear("noite");
        Assert.Equal(VoiceState.Following, v.State);
        Assert.Equal("noite", v.Words[v.LastMatch!.Value].Text);
    }

    [Fact]
    public void Text_helpers()
    {
        Assert.Equal("comeca", TextUtil.Normalize("Começa,"));
        Assert.True(TextUtil.Similarity("noticias", "noticia") > 0.75);
        Assert.Equal(90, TextUtil.ParseDuration("01:30"));
        Assert.Equal(3723, TextUtil.ParseDuration("1:02:03"));
        Assert.Null(TextUtil.ParseDuration("x"));
        Assert.Equal("01:05", TextUtil.FormatMmSs(65));
    }
}

public class SettingsTests
{
    [Fact]
    public void Velocidade_padrao_e_migracao()
    {
        var novo = new PrompterSettings();
        Assert.InRange(novo.LinesPerSecond, 0.5, 0.6); // ≈ 165 palavras/min com 5 palavras por linha
        var antigo = new PrompterSettings { SpeedT = 0.3, MinSpeed = 0.3, MaxSpeed = 3 }.MigrateSpeed();
        Assert.Equal(novo.LinesPerSecond, antigo.LinesPerSecond, 6);
        var escolhido = new PrompterSettings { SpeedT = 0.4, MinSpeed = 0.3, MaxSpeed = 3 }.MigrateSpeed();
        Assert.Equal(0.4, escolhido.SpeedT); // velocidade do usuário não muda
    }
}

public class DisplayTests
{
    [Fact]
    public void Linhas_em_branco_nao_viram_espaco_no_prompter()
    {
        var doc = ScriptDocument.FromPlainText("Primeiro parágrafo.\n\n\nSegundo parágrafo.");
        var html = DisplayHtml.Build(doc, new PrompterSettings());
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(html, "<p").Count);
    }
}

public class VoiceMotionTests
{
    [Fact]
    public void Voice_motion_has_limited_acceleration_and_no_overshoot()
    {
        var s = new PrompterSettings();
        var e = new ScrollEngine(s) { LineHeight = 80, EndY = 1e6, VoiceMode = true, VoiceSmooth = 0.35 };
        e.VoiceTarget = 160;                       // a palavra seguinte está duas linhas abaixo
        double lastV = 0, maxA = 0, maxY = 0;
        for (var i = 0; i < 180; i++)
        {
            e.Tick(1 / 60.0);
            maxA = Math.Max(maxA, Math.Abs(e.Velocity - lastV) * 60); lastV = e.Velocity; maxY = Math.Max(maxY, e.Y);
        }
        Assert.True(maxA <= 80 * 6 + 1, $"aceleração {maxA:0} acima do limite");
        Assert.True(maxY <= 160 + 2, $"passou do alvo: {maxY:0.0}");
        Assert.Equal(160, e.Y, 0);
    }
}
