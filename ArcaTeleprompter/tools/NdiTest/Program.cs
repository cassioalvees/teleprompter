// Teste de fumaça do Arca.Ndi: publica uma fonte NDI com barras animadas por alguns segundos.
using Arca.Ndi;
Console.WriteLine($"NDI disponível: {NdiSender.IsAvailable} · versão {NdiSender.RuntimeVersion} · {NdiSender.RuntimePath}");
if (!NdiSender.IsAvailable) return 1;
var fmt = NdiFormat.Get("720p30");
using var s = new NdiSender("ARCA-TESTE", fmt, false);
var buf = new byte[fmt.Width * fmt.Height * 4];
var secs = args.Length > 0 ? int.Parse(args[0]) : 5;
for (var f = 0; f < secs * 30; f++)
{
    for (var y = 0; y < fmt.Height; y++)
        for (var x = 0; x < fmt.Width; x++)
        {
            var i = (y * fmt.Width + x) * 4; var v = (byte)((x + f * 8) % 256);
            buf[i] = v; buf[i + 1] = (byte)(y % 256); buf[i + 2] = 40; buf[i + 3] = 255;
        }
    s.Send(buf);
    if (f % 30 == 0) Console.WriteLine($"quadro {f} · receptores {s.Connections}");
    Thread.Sleep(33);
}
Console.WriteLine("ok");
return 0;
