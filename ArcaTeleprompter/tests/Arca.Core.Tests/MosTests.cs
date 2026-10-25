using System.Net.Sockets;
using System.Text;
using Arca.Mos;

namespace Arca.Core.Tests;

public class MosTests
{
    private const string RoCreate = """
        <mos><mosID>ARCA.PROMPTER.1</mosID><ncsID>ENPS</ncsID><messageID>1</messageID>
          <roCreate><roID>RO1</roID><roSlug>Jornal da Noite</roSlug>
            <story><storyID>S1</storyID><storySlug>Abertura</storySlug><storyNum>A1</storyNum></story>
            <story><storyID>S2</storyID><storySlug>Chuvas</storySlug></story>
          </roCreate></mos>
        """;
    private const string StorySend = """
        <mos><mosID>ARCA.PROMPTER.1</mosID><ncsID>ENPS</ncsID><messageID>2</messageID>
          <roStorySend><roID>RO1</roID><storyID>S2</storyID><storySlug>Chuvas no litoral</storySlug>
            <storyBody><p>A chuva forte deixou cidades em alerta.</p><p><pi>VT 1:45</pi></p><p>Defesa Civil   registrou   120 mm.</p></storyBody>
          </roStorySend></mos>
        """;

    [Fact]
    public void Running_order_create_story_send_and_element_actions()
    {
        var dev = new MosDevice();
        RunningOrder? last = null;
        dev.RunningOrderChanged += ro => last = ro;
        var ack = dev.Process(RoCreate);
        Assert.Contains("<roStatus>OK</roStatus>", ack);
        Assert.Equal(2, last!.Stories.Count);
        dev.Process(StorySend);
        var s2 = last.Stories[1];
        Assert.Equal("Chuvas no litoral", s2.Slug);
        Assert.Equal(new[] { "A chuva forte deixou cidades em alerta.", "[VT 1:45]", "Defesa Civil registrou 120 mm." }, s2.Paragraphs);
        dev.Process("<mos><mosID>A</mosID><ncsID>ENPS</ncsID><roElementAction operation=\"MOVE\"><roID>RO1</roID><element_target><storyID>S1</storyID></element_target><element_source><storyID>S2</storyID></element_source></roElementAction></mos>");
        Assert.Equal("S2", last.Stories[0].Id);
        dev.Process("<mos><mosID>A</mosID><ncsID>ENPS</ncsID><roElementAction operation=\"DELETE\"><roID>RO1</roID><element_source><storyID>S1</storyID></element_source></roElementAction></mos>");
        Assert.Single(last.Stories);
        var hb = dev.Process("<mos><mosID>A</mosID><ncsID>ENPS</ncsID><heartbeat><time>2026-01-01T00:00:00</time></heartbeat></mos>");
        Assert.Contains("<heartbeat>", hb);
    }

    [Fact]
    public async Task Tcp_ucs2_round_trip_on_upper_port()
    {
        using var dev = new MosDevice { LowerPort = 20540, UpperPort = 20541 };
        var got = new TaskCompletionSource<RunningOrder>();
        dev.RunningOrderChanged += ro => got.TrySetResult(ro);
        dev.Start();
        using var c = new TcpClient();
        await c.ConnectAsync("127.0.0.1", 20541);
        var s = c.GetStream();
        var bytes = Encoding.BigEndianUnicode.GetBytes(RoCreate);
        await s.WriteAsync(bytes);
        var ro = await got.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("Jornal da Noite", ro.Slug);
        var buf = new byte[4096];
        var n = await s.ReadAsync(buf).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains("roAck", Encoding.BigEndianUnicode.GetString(buf, 0, n));
    }
}
