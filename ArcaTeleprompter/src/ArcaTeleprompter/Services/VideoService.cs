using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Arca.Core.Storage;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace Arca.App.Services;

/// <summary>
/// Câmera e gravação (Windows.Media.Capture): prévia no inspetor e gravação MP4 (H.264/AAC) em
/// Vídeos\ARCA Teleprompter. Opcionalmente grava junto com o prompt (Play inicia, pausa para).
/// </summary>
public sealed class VideoService
{
    private readonly MainWindow _main;
    private MediaCapture? _mc;
    private MediaFrameReader? _reader;
    private WriteableBitmap? _wb;
    private DateTime _lastFrame;
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private int _sec;
    private string? _file;

    public VideoService(MainWindow main)
    {
        _main = main;
        _clock.Tick += (_, _) => { _sec++; _main.Vm.RecLabel = Arca.Core.Model.TextUtil.FormatMmSs(_sec); };
    }

    public async Task<List<(string Id, string Name)>> ListCameras()
    {
        try { return (await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture)).Select(d => (d.Id, d.Name)).ToList(); }
        catch (Exception e) { Log.Warn("Câmeras: " + e.Message); return new(); }
    }

    public async Task ToggleCamera(string? id)
    {
        var vm = _main.Vm;
        if (vm.CamOn) { await Off(); return; }
        if (!_main.Allow("video")) return;
        try
        {
            _mc = new MediaCapture();
            await _mc.InitializeAsync(new MediaCaptureInitializationSettings
            {
                VideoDeviceId = id ?? "", StreamingCaptureMode = StreamingCaptureMode.AudioAndVideo,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu, SharingMode = MediaCaptureSharingMode.ExclusiveControl
            });
            var src = _mc.FrameSources.Values.FirstOrDefault(s => s.Info.SourceKind == MediaFrameSourceKind.Color && s.Info.MediaStreamType == Windows.Media.Capture.MediaStreamType.VideoPreview)
                   ?? _mc.FrameSources.Values.FirstOrDefault(s => s.Info.SourceKind == MediaFrameSourceKind.Color);
            if (src != null)
            {
                _reader = await _mc.CreateFrameReaderAsync(src, MediaEncodingSubtypes.Bgra8);
                _reader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
                _reader.FrameArrived += OnFrame;
                await _reader.StartAsync();
            }
            vm.CamOn = true; vm.CamMessage = "";
        }
        catch (UnauthorizedAccessException) { vm.CamMessage = "Acesso à câmera bloqueado nas configurações de privacidade do Windows"; await Off(); }
        catch (Exception e) { vm.CamMessage = "Câmera indisponível: " + e.Message; Log.Error("Câmera", e); await Off(); }
    }

    private void OnFrame(MediaFrameReader r, MediaFrameArrivedEventArgs a)
    {
        var now = DateTime.UtcNow; if ((now - _lastFrame).TotalMilliseconds < 66) return; _lastFrame = now; // ~15 fps de prévia
        using var f = r.TryAcquireLatestFrame();
        var sb = f?.VideoMediaFrame?.SoftwareBitmap;
        if (sb == null) return;
        using var bgra = SoftwareBitmap.Convert(sb, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        var w = bgra.PixelWidth; var h = bgra.PixelHeight;
        var buf = new Windows.Storage.Streams.Buffer((uint)(w * h * 4));
        bgra.CopyToBuffer(buf);
        var bytes = buf.ToArray();
        _main.Dispatcher.BeginInvoke(() =>
        {
            if (_wb == null || _wb.PixelWidth != w || _wb.PixelHeight != h) { _wb = new WriteableBitmap(w, h, 96, 96, PixelFormats.Pbgra32, null); _main.VideoPreview.Source = _wb; }
            _wb.WritePixels(new Int32Rect(0, 0, w, h), bytes, w * 4, 0);
        });
    }

    public async Task Off()
    {
        if (_main.Vm.Recording) await StopRec();
        try { if (_reader != null) { _reader.FrameArrived -= OnFrame; await _reader.StopAsync(); _reader.Dispose(); } } catch { /* */ }
        _reader = null;
        _mc?.Dispose(); _mc = null;
        _main.VideoPreview.Source = null; _wb = null;
        _main.Vm.CamOn = false;
        if (string.IsNullOrEmpty(_main.Vm.CamMessage)) _main.Vm.CamMessage = "Câmera desligada";
    }

    public async Task ToggleRecording()
    {
        if (!_main.Vm.Recording && !_main.Allow("video")) return;
        if (_main.Vm.Recording) await StopRec(); else await StartRec();
    }

    public async Task StartRec()
    {
        var vm = _main.Vm;
        if (_mc == null || !vm.CamOn) { vm.CamMessage = "Ligue a câmera primeiro"; return; }
        if (vm.Recording) return;
        try
        {
            var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "ARCA Teleprompter");
            System.IO.Directory.CreateDirectory(dir);
            var folder = await StorageFolder.GetFolderFromPathAsync(dir);
            var name = $"{System.IO.Path.GetFileNameWithoutExtension(_main.CurrentDocName())} {DateTime.Now:yyyy-MM-dd HH-mm-ss}.mp4";
            var file = await folder.CreateFileAsync(name, CreationCollisionOption.GenerateUniqueName);
            await _mc.StartRecordToStorageFileAsync(MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto), file);
            _file = file.Path; _sec = 0; vm.RecLabel = "00:00"; vm.Recording = true; _clock.Start();
            Log.Info("Gravação iniciada: " + _file);
        }
        catch (Exception e) { vm.CamMessage = "Não foi possível gravar: " + e.Message; Log.Error("Gravação", e); }
    }

    public async Task StopRec()
    {
        var vm = _main.Vm;
        if (!vm.Recording || _mc == null) return;
        _clock.Stop();
        try { await _mc.StopRecordAsync(); } catch (Exception e) { Log.Error("Parar gravação", e); }
        vm.Recording = false; vm.LastRecording = _file;
        _main.Flash("Gravação salva · " + System.IO.Path.GetFileName(_file));
    }
}
