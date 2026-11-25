using NAudio;
using NAudio.Wave;
using SpeechApp.Core.Abstractions;
using SpeechApp.Core.Hatalar;

namespace SpeechApp.Infrastructure.Seslendirme;

// WAV icerigini varsayilan hoparlorden calar (NAudio WaveOut). Her Cal'da yeni oynatici acilir.
// CalmaBitti, Cal'i cagiran thread'in baglamina (arayuz thread'i) gonderilir; Durdur ile kesilen calma bildirilmez.
public sealed class NAudioSesCalar : ISesCalar, IDisposable
{
    private WaveOut? _cikis;
    private WaveFileReader? _okuyucu;
    private SynchronizationContext? _baglam;

    public event Action? CalmaBitti;

    public void Cal(byte[] wav)
    {
        Kapat();
        _baglam = SynchronizationContext.Current;
        var okuyucu = new WaveFileReader(new MemoryStream(wav));
        var cikis = new WaveOut();
        cikis.PlaybackStopped += (_, _) => Bitti(cikis);
        try
        {
            cikis.Init(okuyucu);
            cikis.Play();
        }
        catch (MmException hata)
        {
            cikis.Dispose();
            okuyucu.Dispose();
            throw new SeslendirmeHatasi("Ses çalınamadı: hoparlör ya da kulaklık bulunamadı. Bir ses cihazı bağlayıp tekrar deneyin.", hata);
        }
        _cikis = cikis;
        _okuyucu = okuyucu;
    }

    public void Duraklat() => _cikis?.Pause();

    public void Devam() => _cikis?.Play();   // duraklatilmis oynaticida Play kaldigi yerden devam eder

    public void Durdur() => Kapat();

    public void Dispose() => Kapat();

    // Bu oynatici hala gecerli olansa (Durdur/yeni Cal ile degistirilmediyse) ses kendiliginden bitmistir.
    private void Bitti(WaveOut cikis)
    {
        void Bildir()
        {
            if (!ReferenceEquals(cikis, _cikis))
            {
                return;
            }
            Kapat();
            CalmaBitti?.Invoke();
        }

        if (_baglam is { } baglam)
        {
            baglam.Post(_ => Bildir(), null);
        }
        else
        {
            Bildir();
        }
    }

    private void Kapat()
    {
        WaveOut? cikis = _cikis;
        WaveFileReader? okuyucu = _okuyucu;
        _cikis = null;
        _okuyucu = null;
        cikis?.Stop();
        cikis?.Dispose();
        okuyucu?.Dispose();
    }
}
