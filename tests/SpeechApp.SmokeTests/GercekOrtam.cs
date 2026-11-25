using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SpeechApp.Core.Abstractions;
using SpeechApp.Infrastructure.Platform;
using SpeechApp.Infrastructure.Seslendirme;

namespace SpeechApp.SmokeTests;

// Gercek model ve gercek Turkce ses gerektiren testlerin ortak yardimcisi.
// Model: SPEECHAPP_MODEL ya da repo kokundeki model\ggml-small-q8_0.bin. Yoksa test "Inconclusive" (atlandi) olur.
// Turkce ses (Tolga) yoksa da atlanir: GitHub'in Windows sunucularinda Turkce ses yuklu degil.
public static class GercekOrtam
{
    public static string? ModelYolu()
    {
        string? ozel = Environment.GetEnvironmentVariable(UygulamaYollari.ModelDegiskeni);
        if (!string.IsNullOrWhiteSpace(ozel) && File.Exists(ozel))
        {
            return ozel;
        }
        for (DirectoryInfo? klasor = new(AppContext.BaseDirectory); klasor is not null; klasor = klasor.Parent)
        {
            string aday = Path.Combine(klasor.FullName, "model", UygulamaYollari.ModelAdi);
            if (File.Exists(aday))
            {
                return aday;
            }
        }
        return null;
    }

    public static string ModelYaDaAtla() =>
        ModelYolu() ?? throw new AssertInconclusiveException("Whisper modeli yok (model\\ klasoru ya da SPEECHAPP_MODEL).");

    // Metni Windows'un Turkce sesiyle okutup Whisper'in istedigi bicime (16 kHz, tek kanal, float) cevirir.
    public static async Task<float[]> TurkceSesUretAsync(string metin, IGunlukcu gunlukcu)
    {
        var seslendirici = new WindowsSeslendirici(gunlukcu);
        SesBilgisi ses = seslendirici.TurkceSesler().FirstOrDefault()
            ?? throw new AssertInconclusiveException("Bu bilgisayarda Turkce seslendirme sesi yok.");
        byte[] wav = await seslendirici.SentezleAsync(metin, ses.Kimlik, 1.0, CancellationToken.None);

        using var okuyucu = new WaveFileReader(new MemoryStream(wav));
        ISampleProvider kaynak = okuyucu.ToSampleProvider();
        if (kaynak.WaveFormat.Channels == 2)
        {
            kaynak = kaynak.ToMono();
        }
        var yeniden = new WdlResamplingSampleProvider(kaynak, 16000);
        var ornekler = new List<float>();
        var tampon = new float[16000];
        int okunan;
        while ((okunan = yeniden.Read(tampon)) > 0)   // NAudio 3: Read(Span<float>)
        {
            ornekler.AddRange(tampon.AsSpan(0, okunan));
        }
        return [.. ornekler];
    }
}
