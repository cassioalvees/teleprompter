// Teste manual do monitor web: sobe o servidor com o roteiro de exemplo e rola o texto sozinho.
using Arca.Core.Engine;
using Arca.Core.Model;
using Arca.Core.Net;
using Arca.Server;

var doc = SampleScripts.JornalDaNoite();
var s = new PrompterSettings();
var l = PrompterLayout.Compute(s, 1);
var (il, iw, ih) = l.Indicator(s.IndicatorType);
await using var srv = new PrompterServer { AllowControl = true };
var playing = true;
srv.Command += c => { Console.WriteLine($"comando: {c.Name} {c.Value}"); if (c.Name == "togglePlay") playing = !playing; };
srv.ViewersChanged += n => Console.WriteLine($"navegadores: {n}");
await srv.StartAsync(8420, https: args.Contains("--https"));
Console.WriteLine("http://localhost:8420/ · " + string.Join(", ", PrompterServer.LanAddresses()));
srv.PublishHtml(DisplayHtml.Build(doc, s));
double y = 0; var t0 = DateTime.Now;
while (true)
{
    var snap = new PrompterSnapshot { Bg = s.Background, Fg = s.Foreground, FontFamily = "Atkinson Hyperlegible", FontSize = l.FontSize, LineH = s.LineHeight, PadL = l.PaddingLeft, PadR = l.PaddingRight,
        Align = s.Align, ReadPct = s.ReadPct, OvType = s.OverlayType, OvOp = s.OverlayOpacity, IndType = s.IndicatorType, IndColor = s.IndicatorColor, IndLeft = il, IndW = iw, IndH = ih,
        Pres = s.Presenters, PresNames = true, Live = playing, Playing = playing, SpeedT = s.SpeedT, Speed = "1.11 linhas/s", Block = "Abertura", BlockLeft = "00:12", Stamp = "00:03",
        Clock = DateTime.Now.ToString("HH:mm:ss"), WebCtrl = true, DocName = "Jornal da Noite.tps" };
    srv.PublishState(snap);
    var v = playing ? s.LinesPerSecond * l.LineHeightPx : 0;
    for (var i = 0; i < 10; i++) { y += v / 60; srv.PublishPosition(y, v, -1, 0); await Task.Delay(16); }
    if ((DateTime.Now - t0).TotalMinutes > 30) break;
}
