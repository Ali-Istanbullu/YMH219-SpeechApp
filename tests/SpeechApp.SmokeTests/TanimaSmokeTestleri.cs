using SpeechApp.Core.Hatalar;
using SpeechApp.Infrastructure.Tanima;

namespace SpeechApp.SmokeTests;

[TestClass]
[TestCategory("Smoke")]
public sealed class TanimaSmokeTestleri
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task ModelYoksa_AnlasilirTurkceHata()
    {
        using var tanici = new WhisperTanici(Path.Combine(Path.GetTempPath(), "olmayan-model.bin"), new BosGunlukcu());

        TanimaHatasi hata = await Assert.ThrowsExactlyAsync<TanimaHatasi>(() => tanici.HazirlaAsync(CancellationToken.None));

        StringAssert.Contains(hata.Message, "bulunamadı");
    }

    [TestMethod]
    public async Task BozukModel_AnlasilirTurkceHata()
    {
        string bozuk = Path.Combine(Path.GetTempPath(), $"bozuk-{Guid.NewGuid():N}.bin");
        await File.WriteAllBytesAsync(bozuk, new byte[4096]);
        try
        {
            using var tanici = new WhisperTanici(bozuk, new BosGunlukcu());

            TanimaHatasi hata = await Assert.ThrowsExactlyAsync<TanimaHatasi>(() => tanici.HazirlaAsync(CancellationToken.None));

            StringAssert.Contains(hata.Message, "bozuk");
        }
        finally
        {
            File.Delete(bozuk);
        }
    }

    // Uctan uca: Windows Turkce sesiyle okunan cumle Whisper ile geri metne cevrilir.
    [TestMethod]
    public async Task TurkceSes_OkutulupGeriMetneCevrilir()
    {
        string model = GercekOrtam.ModelYaDaAtla();
        var gunlukcu = new BosGunlukcu();
        float[] ses = await GercekOrtam.TurkceSesUretAsync("Bugün hava çok güzel, akşam parkta yürüyüş yapacağız.", gunlukcu);
        using var tanici = new WhisperTanici(model, gunlukcu);

        string metin = await tanici.CevirAsync(ses, CancellationToken.None);

        TestContext.WriteLine($"Whisper: \"{metin}\"");
        foreach (string satir in gunlukcu.Satirlar)
        {
            TestContext.WriteLine(satir);
        }
        string kucuk = metin.ToLowerInvariant();
        Assert.IsTrue(kucuk.Contains("hava") && kucuk.Contains("park"), $"anahtar kelimeler yok: {metin}");
    }
}
