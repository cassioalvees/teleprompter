// Receptor NDI de teste: procura fontes na rede, conecta na que contém o nome pedido e salva um quadro em PNG.
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

static class P
{
    const string L = "Processing.NDI.Lib.x64.dll";
    [StructLayout(LayoutKind.Sequential)] struct FindCreate { [MarshalAs(UnmanagedType.U1)] public bool show_local; public IntPtr groups; public IntPtr extra; }
    [StructLayout(LayoutKind.Sequential)] struct Source { public IntPtr name; public IntPtr url; }
    [StructLayout(LayoutKind.Sequential)] struct RecvCreate { public Source src; public int color; public int bandwidth; [MarshalAs(UnmanagedType.U1)] public bool fields; public IntPtr name; }
    [StructLayout(LayoutKind.Sequential)] struct Video { public int xres, yres; public uint fourcc; public int n, d; public float aspect; public int format; public long timecode; public IntPtr data; public int stride; public IntPtr meta; public long ts; }
    [DllImport(L)] static extern bool NDIlib_initialize();
    [DllImport(L)] static extern IntPtr NDIlib_find_create_v2(ref FindCreate c);
    [DllImport(L)] static extern bool NDIlib_find_wait_for_sources(IntPtr f, uint ms);
    [DllImport(L)] static extern IntPtr NDIlib_find_get_current_sources(IntPtr f, out uint n);
    [DllImport(L)] static extern IntPtr NDIlib_recv_create_v3(ref RecvCreate c);
    [DllImport(L)] static extern int NDIlib_recv_capture_v2(IntPtr r, ref Video v, IntPtr a, IntPtr m, uint ms);
    [DllImport(L)] static extern void NDIlib_recv_free_video_v2(IntPtr r, ref Video v);

    [STAThread]
    static int Main(string[] args)
    {
        NativeLibrary.SetDllImportResolver(typeof(P).Assembly, (n, a, s) => n == L ? NativeLibrary.Load(Path.Combine(Environment.GetEnvironmentVariable("NDI_RUNTIME_DIR_V6")!, L)) : IntPtr.Zero);
        var want = args.Length > 0 ? args[0] : "ARCA";
        NDIlib_initialize();
        var fc = new FindCreate { show_local = true };
        var f = NDIlib_find_create_v2(ref fc);
        Source? hit = null;
        for (var i = 0; i < 10 && hit == null; i++)
        {
            NDIlib_find_wait_for_sources(f, 1000);
            var p = NDIlib_find_get_current_sources(f, out var n);
            for (var k = 0; k < n; k++)
            {
                var s = Marshal.PtrToStructure<Source>(p + k * Marshal.SizeOf<Source>());
                var name = Marshal.PtrToStringUTF8(s.name)!;
                if (i == 0 || hit == null) Console.WriteLine("fonte: " + name);
                if (name.Contains(want, StringComparison.OrdinalIgnoreCase)) hit = s;
            }
        }
        if (hit == null) { Console.WriteLine("fonte não encontrada"); return 1; }
        var rc = new RecvCreate { src = hit.Value, color = 0, bandwidth = 100 };
        var r = NDIlib_recv_create_v3(ref rc);
        var v = new Video();
        // --movimento N [saida.csv]: recebe N quadros e mede quanto o texto andou entre um quadro e o seguinte
        // (correlação do perfil de brilho das linhas, com precisão abaixo de 1 pixel) — degraus, paradas e trancos.
        var mv = Array.IndexOf(args, "--movimento");
        if (mv >= 0) return Movement(r, int.Parse(args[mv + 1]), args.Length > mv + 2 ? args[mv + 2] : null);
        // --medir N: recebe N quadros e mede a regularidade (intervalo entre quadros).
        var mi = Array.IndexOf(args, "--medir");
        if (mi >= 0)
        {
            var total = int.Parse(args[mi + 1]); var empty = 0; var sw = System.Diagnostics.Stopwatch.StartNew(); var gaps = new List<double>(); double last = -1; var got = 0;
            while (got < total && sw.Elapsed.TotalSeconds < total / 10.0 + 10)
            {
                if (NDIlib_recv_capture_v2(r, ref v, IntPtr.Zero, IntPtr.Zero, 1000) != 1) continue;
                var now = sw.Elapsed.TotalMilliseconds; if (last >= 0) gaps.Add(now - last); last = now; got++;
                // Quadro vazio = nenhum pixel claro numa amostra em grade (texto sumiu).
                unsafe { var px = (byte*)v.data; var lit = 0; for (var yy = 0; yy < v.yres; yy += 8) for (var xx = 0; xx < v.xres; xx += 8) { var q = px + yy * v.stride + xx * 4; if (q[0] + q[1] + q[2] > 300) lit++; } if (lit < 40) empty++; }
                if (got == total / 2) { var bmp2 = BitmapSource.Create(v.xres, v.yres, 96, 96, PixelFormats.Bgra32, null, v.data, v.stride * v.yres, v.stride); var e2 = new PngBitmapEncoder(); e2.Frames.Add(BitmapFrame.Create(bmp2)); using var f2 = File.Create(args.Length > 1 && !args[1].StartsWith("--") ? args[1] : "ndi-frame.png"); e2.Save(f2); }
                NDIlib_recv_free_video_v2(r, ref v);
            }
            if (gaps.Count == 0) { Console.WriteLine("sem quadros"); return 2; }
            var avg = gaps.Average(); var sd = Math.Sqrt(gaps.Average(g => (g - avg) * (g - avg)));
            Console.WriteLine($"{got} quadros · média {avg:0.0} ms ({1000 / avg:0.00} qps) · desvio {sd:0.0} ms · maior intervalo {gaps.Max():0.0} ms · acima de 1,5× o período: {gaps.Count(g => g > avg * 1.5)} · quadros sem texto: {empty}");
            return 0;
        }
        for (var i = 0; i < 100; i++)
        {
            if (NDIlib_recv_capture_v2(r, ref v, IntPtr.Zero, IntPtr.Zero, 1000) != 1) continue;
            Console.WriteLine($"quadro {v.xres}x{v.yres} fourcc={(char)(v.fourcc & 255)}{(char)(v.fourcc >> 8 & 255)}{(char)(v.fourcc >> 16 & 255)}{(char)(v.fourcc >> 24)} {v.n}/{v.d} fps");
            var bmp = BitmapSource.Create(v.xres, v.yres, 96, 96, PixelFormats.Bgra32, null, v.data, v.stride * v.yres, v.stride);
            var enc = new PngBitmapEncoder(); enc.Frames.Add(BitmapFrame.Create(bmp));
            using (var fs = File.Create(args.Length > 1 ? args[1] : "ndi-frame.png")) enc.Save(fs);
            NDIlib_recv_free_video_v2(r, ref v);
            Console.WriteLine("PNG salvo");
            return 0;
        }
        Console.WriteLine("sem quadros");
        return 2;
    }

    /// <summary>
    /// Perfil de brilho por linha (colunas do centro, longe da seta e das bordas). Texto rolando = o perfil se desloca;
    /// o deslocamento entre dois quadros é o que minimiza a diferença dos perfis (refinado por parábola).
    /// </summary>
    static unsafe double[] Profile(ref Video v)
    {
        var p = new double[v.yres]; var px = (byte*)v.data;
        int x0 = v.xres / 5, x1 = v.xres * 9 / 10;
        for (var y = 0; y < v.yres; y++)
        {
            double s = 0; var row = px + (long)y * v.stride;
            for (var x = x0; x < x1; x += 3) { var q = row + x * 4; s += q[0] + q[1] + q[2]; }
            p[y] = s;
        }
        return p;
    }

    static double Shift(double[] a, double[] b, int max)
    {
        // b(y) ≈ a(y + d): texto subindo → d > 0 (pixels que o texto andou para cima).
        int n = a.Length, lo = max, hi = n - max;
        double Cost(int d) { double c = 0; for (var y = lo; y < hi; y++) { var e = b[y] - a[y + d]; c += e * e; } return c; }
        int best = 0; var bc = double.MaxValue; var costs = new Dictionary<int, double>();
        for (var d = -max; d <= max; d++) { var c = Cost(d); costs[d] = c; if (c < bc) { bc = c; best = d; } }
        if (best <= -max || best >= max) return best;
        double c0 = costs[best - 1], c1 = costs[best], c2 = costs[best + 1], den = c0 - 2 * c1 + c2;
        return best + (den > 0 ? 0.5 * (c0 - c2) / den : 0);
    }

    static int Movement(IntPtr r, int total, string? csv)
    {
        var v = new Video(); double[]? prev = null; long prevTc = 0; var got = 0;
        var rows = new List<(double T, double Dt, double Px, double V)>();
        var sw = System.Diagnostics.Stopwatch.StartNew(); double t0 = -1;
        while (got < total && sw.Elapsed.TotalSeconds < total / 10.0 + 15)
        {
            if (NDIlib_recv_capture_v2(r, ref v, IntPtr.Zero, IntPtr.Zero, 1000) != 1) continue;
            var prof = Profile(ref v); var tc = v.timecode; got++;
            if (prev != null && tc > prevTc)
            {
                var dt = (tc - prevTc) / 1e7;   // timecode NDI em 100 ns
                var px = Shift(prev, prof, 80);
                if (t0 < 0) t0 = prevTc / 1e7;
                rows.Add((tc / 1e7 - t0, dt, px, px / dt));
            }
            prev = prof; prevTc = tc;
            NDIlib_recv_free_video_v2(r, ref v);
        }
        if (rows.Count < 3) { Console.WriteLine("quadros insuficientes"); return 2; }
        if (csv != null) File.WriteAllLines(csv, new[] { "t;dt_ms;px;px_s" }.Concat(rows.Select(x => FormattableString.Invariant($"{x.T:0.000};{x.Dt * 1000:0.0};{x.Px:0.00};{x.V:0.0}"))));
        // Saltos de velocidade entre quadros seguidos, paradas (0 px entre quadros andando) e quadros para trás.
        var moving = rows.Where(x => Math.Abs(x.V) > 5).ToList();
        var jumps = rows.Zip(rows.Skip(1), (a, b) => Math.Abs(b.V - a.V)).ToList();
        var stalls = 0; for (var i = 1; i + 1 < rows.Count; i++) if (Math.Abs(rows[i].Px) < 0.15 && rows[i - 1].Px > 1 && rows[i + 1].Px > 1) stalls++;
        var back = rows.Count(x => x.Px < -0.5);
        var vmax = rows.Max(x => x.V);
        var dts = rows.Select(x => x.Dt * 1000).ToList();
        // Ruído (tremor) = diferença de cada quadro para a média dos vizinhos, nos trechos de velocidade estável.
        var jitter = new List<double>(); for (var i = 1; i + 1 < rows.Count; i++) jitter.Add(Math.Abs(rows[i].Px - (rows[i - 1].Px + rows[i + 1].Px) / 2));
        jitter.Sort();
        Console.WriteLine($"{got} quadros · intervalo {dts.Average():0.0} ms (máx {dts.Max():0.0}) · velocidade máx {vmax:0} px/s");
        Console.WriteLine($"maior salto de velocidade entre quadros {jumps.Max():0} px/s · mediana {jumps.OrderBy(x => x).ElementAt(jumps.Count / 2):0.0} px/s");
        Console.WriteLine($"tremor (px fora da média dos vizinhos): mediana {jitter[jitter.Count / 2]:0.00} · p95 {jitter[(int)(jitter.Count * 0.95)]:0.00} · máx {jitter.Max():0.00}");
        Console.WriteLine($"paradas no meio do movimento: {stalls} · quadros para trás: {back} · quadros em movimento: {moving.Count}");
        return 0;
    }
}
