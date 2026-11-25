using SpeechApp.Core.Abstractions;
using SpeechApp.Core.Hatalar;

namespace SpeechApp.Core.Seslendirme;

// "Metni Seslendir" bolumunun mantigi: Baslat, Duraklat/Devam, Durdur ve durum takibi.
// Arayuz thread'inden kullanilir; DurumDegisti de oradan gelir.
public sealed class SeslendirmeYoneticisi
{
    public const double EnDusukHiz = 0.5;
    public const double EnYuksekHiz = 2.0;

    private readonly ISeslendirici _seslendirici;
    private readonly ISesCalar _calar;
    private readonly IGunlukcu _gunlukcu;
    private CancellationTokenSource? _hazirlamaIptali;

    public SeslendirmeYoneticisi(ISeslendirici seslendirici, ISesCalar calar, IGunlukcu gunlukcu)
    {
        _seslendirici = seslendirici;
        _calar = calar;
        _gunlukcu = gunlukcu;
        _calar.CalmaBitti += () => DurumYap(SeslendirmeDurumu.Hazir);
    }

    public SeslendirmeDurumu Durum { get; private set; } = SeslendirmeDurumu.Hazir;

    public event Action? DurumDegisti;

    // Yuklu Turkce sesler. Hic yoksa kullaniciya nereden eklenecegini soyleyen hata.
    public IReadOnlyList<SesBilgisi> SesleriGetir()
    {
        IReadOnlyList<SesBilgisi> sesler = _seslendirici.TurkceSesler();
        if (sesler.Count == 0)
        {
            throw new SeslendirmeHatasi("Bilgisayarda Türkçe seslendirme sesi yok. Windows Ayarlar → Saat ve dil → " +
                "Konuşma → \"Ses ekle\" bölümünden Türkçe sesi (Tolga) ekleyip uygulamayı yeniden açın.");
        }
        return sesler;
    }

    // Metni okumaya baslar. Bos metin ve motor hatalari SeslendirmeHatasi olarak cagirana gider.
    public async Task BaslatAsync(string metin, string sesKimligi, double hiz)
    {
        if (Durum != SeslendirmeDurumu.Hazir)
        {
            throw new InvalidOperationException($"Seslendirme zaten suruyor ({Durum}).");   // bug: buton acik kalmis
        }
        if (string.IsNullOrWhiteSpace(metin))
        {
            throw new SeslendirmeHatasi("Okunacak metin yok. Önce kutuya bir metin yazın.");
        }

        using var iptal = new CancellationTokenSource();
        _hazirlamaIptali = iptal;
        DurumYap(SeslendirmeDurumu.Hazirlaniyor);
        byte[] wav;
        try
        {
            wav = await _seslendirici.SentezleAsync(metin.Trim(), sesKimligi, Math.Clamp(hiz, EnDusukHiz, EnYuksekHiz), iptal.Token);
        }
        catch (OperationCanceledException) when (iptal.IsCancellationRequested)
        {
            return;   // hazirlanirken Durdur'a basildi; durum zaten Hazir
        }
        catch (SeslendirmeHatasi)
        {
            DurumYap(SeslendirmeDurumu.Hazir);
            throw;
        }
        finally
        {
            _hazirlamaIptali = null;
        }

        if (iptal.IsCancellationRequested)
        {
            return;
        }
        try
        {
            _calar.Cal(wav);
        }
        catch (SeslendirmeHatasi)
        {
            DurumYap(SeslendirmeDurumu.Hazir);
            throw;
        }
        DurumYap(SeslendirmeDurumu.Okunuyor);
        _gunlukcu.Bilgi($"Seslendirme basladi ({metin.Length} karakter).");   // metnin kendisi loglanmaz
    }

    // Okunuyorsa duraklatir, duraklatildiysa devam ettirir.
    public void DuraklatVeyaDevamEt()
    {
        switch (Durum)
        {
            case SeslendirmeDurumu.Okunuyor:
                _calar.Duraklat();
                DurumYap(SeslendirmeDurumu.Duraklatildi);
                break;
            case SeslendirmeDurumu.Duraklatildi:
                _calar.Devam();
                DurumYap(SeslendirmeDurumu.Okunuyor);
                break;
        }
    }

    // Ne yapiliyorsa keser (hazirlama dahil). Hazir'dayken bir sey yapmaz.
    public void Durdur()
    {
        switch (Durum)
        {
            case SeslendirmeDurumu.Hazirlaniyor:
                _hazirlamaIptali?.Cancel();
                break;
            case SeslendirmeDurumu.Okunuyor:
            case SeslendirmeDurumu.Duraklatildi:
                _calar.Durdur();
                break;
            default:
                return;
        }
        DurumYap(SeslendirmeDurumu.Hazir);
    }

    private void DurumYap(SeslendirmeDurumu yeni)
    {
        if (Durum == yeni)
        {
            return;
        }
        Durum = yeni;
        DurumDegisti?.Invoke();
    }
}
