using System.Runtime.InteropServices;
using SpeechApp.Core.Abstractions;
using SpeechApp.Core.Hatalar;
using Windows.Media.SpeechSynthesis;

namespace SpeechApp.Infrastructure.Seslendirme;

// Windows'un kendi seslendirme motoru (Windows.Media.SpeechSynthesis).
// Neden System.Speech degil: System.Speech sadece eski (SAPI) sesleri gorur; Turkce ses "Microsoft Tolga"
// Ayarlar'dan eklenen yeni (OneCore) seslerdendir ve sadece bu API ile kullanilir.
public sealed class WindowsSeslendirici : ISeslendirici
{
    private readonly IGunlukcu _gunlukcu;

    public WindowsSeslendirici(IGunlukcu gunlukcu)
    {
        _gunlukcu = gunlukcu;
    }

    public IReadOnlyList<SesBilgisi> TurkceSesler()
    {
        try
        {
            return SpeechSynthesizer.AllVoices
                .Where(ses => ses.Language.StartsWith("tr", StringComparison.OrdinalIgnoreCase))
                .Select(ses => new SesBilgisi(ses.Id, ses.DisplayName))
                .ToList();
        }
        catch (COMException hata)
        {
            // Konusma bileseni olmayan Windows surumleri: ses yok sayilir, ekrana "Turkce ses yok" mesaji gider.
            _gunlukcu.Hata(hata, "Windows ses listesi okunamadi.");
            return [];
        }
    }

    public async Task<byte[]> SentezleAsync(string metin, string sesKimligi, double hiz, CancellationToken iptal)
    {
        VoiceInformation ses = SpeechSynthesizer.AllVoices.FirstOrDefault(s => s.Id == sesKimligi)
            ?? throw new SeslendirmeHatasi("Seçilen ses artık bilgisayarda yüklü değil. Uygulamayı yeniden açıp başka bir ses seçin.");

        using var motor = new SpeechSynthesizer { Voice = ses };
        motor.Options.SpeakingRate = hiz;   // 1 = normal (Windows 0,5-6 arasini kabul eder)
        try
        {
            using SpeechSynthesisStream akis = await motor.SynthesizeTextToStreamAsync(metin).AsTask(iptal);
            await using Stream okuyucu = akis.AsStreamForRead();
            using var bellek = new MemoryStream();
            await okuyucu.CopyToAsync(bellek, iptal);
            return bellek.ToArray();   // WAV dosyasi icerigi
        }
        catch (COMException hata)
        {
            throw new SeslendirmeHatasi("Windows seslendirme motoru metni sese çeviremedi. Metni kısaltıp tekrar deneyin.", hata);
        }
    }
}
