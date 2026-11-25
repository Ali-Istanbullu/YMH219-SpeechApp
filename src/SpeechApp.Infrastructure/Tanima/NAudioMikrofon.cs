using NAudio;
using NAudio.Wave;
using SpeechApp.Core.Abstractions;
using SpeechApp.Core.Hatalar;

namespace SpeechApp.Infrastructure.Tanima;

// Mikrofondan 16 kHz, tek kanal, 16 bit ses alir (NAudio WaveIn) ve -1..1 float'a cevirir.
// Windows ses surucusu ornekleme hizini kendisi donusturur. SesGeldi kayit thread'inden gelir.
public sealed class NAudioMikrofon : IMikrofon, IDisposable
{
    private const int OrnekHizi = 16000;
    private const int BlokMs = 100;
    private const string IzinYolu = "Windows Ayarlar → Gizlilik ve güvenlik → Mikrofon";

    private WaveIn? _kayit;

    public event Action<float[]>? SesGeldi;
    public event Action<Exception>? KayitKesildi;

    public IReadOnlyList<string> Cihazlar()
    {
        var adlar = new List<string>();
        for (int i = 0; i < WaveIn.DeviceCount; i++)
        {
            adlar.Add(WaveIn.GetCapabilities(i).ProductName);
        }
        return adlar;
    }

    public void Baslat(int cihazNo)
    {
        if (WaveIn.DeviceCount == 0)
        {
            throw new MikrofonHatasi("Bilgisayarda mikrofon bulunamadı. Bir mikrofon bağlayıp uygulamayı yeniden açın. " +
                $"Mikrofon takılıysa {IzinYolu} bölümünde mikrofon erişimi kapalı olabilir.");
        }

        var kayit = new WaveIn
        {
            DeviceNumber = Math.Clamp(cihazNo, 0, WaveIn.DeviceCount - 1),
            WaveFormat = new WaveFormat(OrnekHizi, 16, 1),
            BufferMilliseconds = BlokMs
        };
        kayit.DataAvailable += (_, e) => SesGeldi?.Invoke(FloataCevir(e.Buffer, e.BytesRecorded));
        kayit.RecordingStopped += (_, e) =>
        {
            if (e.Exception is not null && ReferenceEquals(kayit, _kayit))
            {
                KayitKesildi?.Invoke(e.Exception);
            }
        };
        try
        {
            kayit.StartRecording();
        }
        catch (MmException hata)
        {
            kayit.Dispose();
            throw new MikrofonHatasi($"Mikrofon açılamadı ({hata.Result}). Mikrofon başka bir uygulamada kullanılıyor olabilir " +
                $"ya da {IzinYolu} bölümünde \"Masaüstü uygulamalarının mikrofona erişmesine izin ver\" kapalı olabilir.", hata);
        }
        _kayit = kayit;
    }

    public void Durdur()
    {
        WaveIn? kayit = _kayit;
        _kayit = null;   // once null: durdururken gelen RecordingStopped "kesildi" sayilmasin
        if (kayit is null)
        {
            return;
        }
        kayit.StopRecording();
        kayit.Dispose();
    }

    public void Dispose() => Durdur();

    // 16 bit isaretli tam sayi (little-endian) -> -1..1 float
    private static float[] FloataCevir(byte[] tampon, int bayt)
    {
        var ornekler = new float[bayt / 2];
        for (int i = 0; i < ornekler.Length; i++)
        {
            ornekler[i] = BitConverter.ToInt16(tampon, i * 2) / 32768f;
        }
        return ornekler;
    }
}
