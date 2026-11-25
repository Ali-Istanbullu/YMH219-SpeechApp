using SpeechApp.Core.Tanima;

namespace SpeechApp.UnitTests;

[TestClass]
public sealed class SessizlikAyiriciTestleri
{
    private static List<float[]> Ayir(float[] ses, SessizlikAyirici? ayirici = null)
    {
        ayirici ??= new SessizlikAyirici();
        var parcalar = new List<float[]>();
        foreach (float[] blok in SesUretici.Bloklar(ses))
        {
            parcalar.AddRange(ayirici.Ekle(blok));
        }
        if (ayirici.Bitir() is { } son)
        {
            parcalar.Add(son);
        }
        return parcalar;
    }

    [TestMethod]
    public void IkiCumleArasindakiSessizlik_IkiParcaYapar()
    {
        float[] ses = SesUretici.Birlestir(
            SesUretici.Sessizlik(500), SesUretici.Konusma(1000), SesUretici.Sessizlik(1000),
            SesUretici.Konusma(600), SesUretici.Sessizlik(1000));

        List<float[]> parcalar = Ayir(ses);

        Assert.HasCount(2, parcalar);
        // 1. parca: 300 ms on ses + 1000 ms konusma + 700 ms sessizlik (bitis) = 2000 ms
        Assert.HasCount(2000 * 16, parcalar[0]);
        Assert.HasCount((300 + 600 + 700) * 16, parcalar[1]);
    }

    [TestMethod]
    public void KisaSessizlik_ParcayiBolmez()
    {
        float[] ses = SesUretici.Birlestir(
            SesUretici.Konusma(800), SesUretici.Sessizlik(400), SesUretici.Konusma(800), SesUretici.Sessizlik(1000));

        Assert.HasCount(1, Ayir(ses), "kelimeler arasi kisa duraklama cumleyi bolmemeli");
    }

    [TestMethod]
    public void KisaTik_VeSessizlik_ParcaUretmez()
    {
        float[] ses = SesUretici.Birlestir(
            SesUretici.Sessizlik(500), SesUretici.Konusma(90), SesUretici.Sessizlik(1000), SesUretici.Sessizlik(3000));

        Assert.IsEmpty(Ayir(ses));
    }

    [TestMethod]
    public void UzunKonusma_25SanideZorlaKesilir_KalaniBitirdeGelir()
    {
        float[] ses = SesUretici.Konusma(30_000);

        List<float[]> parcalar = Ayir(ses);

        Assert.HasCount(2, parcalar);
        Assert.HasCount(25_000 * 16, parcalar[0]);
        Assert.HasCount(5_000 * 16, parcalar[1]);
    }

    [TestMethod]
    public void KayitSessizlikGelmedenBiterse_YarimParcaBitirdeDoner()
    {
        var ayirici = new SessizlikAyirici();
        float[] ses = SesUretici.Konusma(1000);

        foreach (float[] blok in SesUretici.Bloklar(ses, 1234))   // cerceveye denk gelmeyen blok boyu
        {
            Assert.IsEmpty(ayirici.Ekle(blok));
        }
        float[]? son = ayirici.Bitir();

        Assert.IsNotNull(son);
        Assert.HasCount(16000, son, "yarim cerceve dahil hic ornek kaybolmamali");
        Assert.IsNull(ayirici.Bitir(), "ikinci Bitir bos donmeli");
    }
}
