using System.Diagnostics;
using SpeechApp.Core.Tanima;
using SpeechApp.Infrastructure.Tanima;
using SpeechApp.SmokeTests;

namespace SpeechApp.PerformanceTests;

[TestClass]
[TestCategory("Performans")]
public sealed class PerformansTestleri
{
    public TestContext TestContext { get; set; } = null!;

    // Sessizlik ayirici mikrofonun yetisebilecegi hizda mi? 10 dakikalik ses (konusma/sessizlik donusumlu).
    [TestMethod]
    public void SessizlikAyirici_OnDakikalikSes()
    {
        const int EsikMs = 1000;
        const int OrnekHizi = 16000;
        var ses = new float[10 * 60 * OrnekHizi];
        for (int i = 0; i < ses.Length; i++)
        {
            bool konusma = i / OrnekHizi % 3 != 2;   // 2 sn konusma, 1 sn sessizlik
            ses[i] = konusma ? 0.3f * MathF.Sin(2 * MathF.PI * 220 * i / OrnekHizi) : 0f;
        }
        var ayirici = new SessizlikAyirici();

        Stopwatch sure = Stopwatch.StartNew();
        int parca = 0;
        for (int i = 0; i < ses.Length; i += 1600)
        {
            parca += ayirici.Ekle(ses.AsSpan(i, Math.Min(1600, ses.Length - i))).Count;
        }
        sure.Stop();

        TestContext.WriteLine($"10 dk ses: {parca} parca, {sure.ElapsedMilliseconds} ms. Esik {EsikMs} ms.");
        Assert.AreEqual(200, parca);
        Assert.IsLessThan(EsikMs, sure.ElapsedMilliseconds);
    }

    // Whisper small-q8_0 ile bir cumlenin cevrilme suresi / ses suresi (gercek zaman orani).
    // Ali'nin bilgisayarinda (AMD Athlon Silver 7120U, 2 cekirdek, 25.11) olculen: 3,79 ve 3,9 kati. Esik bunun ~1,5 kati.
    [TestMethod]
    public async Task Whisper_CumleCevirmeSuresi()
    {
        const double EnFazlaOran = 6.0;
        string model = GercekOrtam.ModelYaDaAtla();
        var gunlukcu = new BosGunlukcu();
        float[] ses = await GercekOrtam.TurkceSesUretAsync("Bu metni dosyaya kaydedip öğretmene göndereceğim.", gunlukcu);
        using var tanici = new WhisperTanici(model, gunlukcu);

        Stopwatch yukleme = Stopwatch.StartNew();
        await tanici.HazirlaAsync(CancellationToken.None);
        yukleme.Stop();
        Stopwatch cevirme = Stopwatch.StartNew();
        string metin = await tanici.CevirAsync(ses, CancellationToken.None);
        cevirme.Stop();

        double sesSn = ses.Length / 16000.0;
        double oran = cevirme.Elapsed.TotalSeconds / sesSn;
        TestContext.WriteLine($"Model yukleme {yukleme.Elapsed.TotalSeconds:0.0} sn; ses {sesSn:0.0} sn, cevirme " +
            $"{cevirme.Elapsed.TotalSeconds:0.0} sn (oran {oran:0.00}). Esik {EnFazlaOran}. Cekirdek: {Environment.ProcessorCount}.");
        TestContext.WriteLine($"Whisper: \"{metin}\"");
        Assert.IsLessThan(EnFazlaOran, oran);
    }
}
