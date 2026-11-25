using SpeechApp.Core.Hatalar;
using SpeechApp.Core.Tanima;
using SpeechApp.UnitTests.Fakes;

namespace SpeechApp.UnitTests;

[TestClass]
public sealed class TanimaYoneticisiTestleri
{
    private static readonly float[] IkiCumle = SesUretici.Birlestir(
        SesUretici.Konusma(1000), SesUretici.Sessizlik(1000), SesUretici.Konusma(1000), SesUretici.Sessizlik(1000));

    private static void Gonder(SahteMikrofon mikrofon, float[] ses)
    {
        foreach (float[] blok in SesUretici.Bloklar(ses))
        {
            mikrofon.Gonder(blok);
        }
    }

    [TestMethod]
    public async Task Konusma_ParcaParcaCevrilir_ZamanDamgasiylaListelenir()
    {
        var mikrofon = new SahteMikrofon();
        var tanici = new SahteTanici("Merhaba.", "Nasılsın?");
        var yonetici = new TanimaYoneticisi(mikrofon, tanici, new SahteSaat(), new SahteGunlukcu());
        var gelenler = new List<string>();
        yonetici.SonucEklendi += s => gelenler.Add(s.Metin);

        yonetici.Baslat(0);
        Assert.AreEqual(TanimaDurumu.Dinleniyor, yonetici.Durum);
        Assert.IsTrue(mikrofon.Acik);
        Gonder(mikrofon, IkiCumle);
        await yonetici.DurdurAsync();

        Assert.AreEqual(TanimaDurumu.Hazir, yonetici.Durum);
        Assert.IsFalse(mikrofon.Acik);
        CollectionAssert.AreEqual(new[] { "Merhaba.", "Nasılsın?" }, gelenler);
        Assert.AreEqual("[14:03:20] Merhaba.\r\n[14:03:21] Nasılsın?\r\n", TanimaDokumu.Olustur(yonetici.Sonuclar));
    }

    [TestMethod]
    public async Task Durdur_SessizlikBeklemedenEldeKalaniDaCevirir()
    {
        var mikrofon = new SahteMikrofon();
        var tanici = new SahteTanici("Son cümle");
        var yonetici = new TanimaYoneticisi(mikrofon, tanici, new SahteSaat(), new SahteGunlukcu());

        yonetici.Baslat(0);
        Gonder(mikrofon, SesUretici.Konusma(1500));   // sessizlik yok
        await yonetici.DurdurAsync();

        Assert.HasCount(1, yonetici.Sonuclar);
        Assert.AreEqual("Son cümle", yonetici.Sonuclar[0].Metin);
    }

    [TestMethod]
    public async Task BosVeyaBoslukluSonuc_ListeyeEklenmez()
    {
        var mikrofon = new SahteMikrofon();
        var yonetici = new TanimaYoneticisi(mikrofon, new SahteTanici("  ", "Tamam"), new SahteSaat(), new SahteGunlukcu());

        yonetici.Baslat(0);
        Gonder(mikrofon, IkiCumle);
        await yonetici.DurdurAsync();

        Assert.HasCount(1, yonetici.Sonuclar);
        Assert.AreEqual("Tamam", yonetici.Sonuclar[0].Metin);
    }

    [TestMethod]
    public void MikrofonAcilamazsa_HataCagiranaGider_DurumHazirKalir()
    {
        var mikrofon = new SahteMikrofon { BaslatHataVersin = true };
        var yonetici = new TanimaYoneticisi(mikrofon, new SahteTanici(), new SahteSaat(), new SahteGunlukcu());

        Assert.ThrowsExactly<MikrofonHatasi>(() => yonetici.Baslat(0));
        Assert.AreEqual(TanimaDurumu.Hazir, yonetici.Durum);
    }

    [TestMethod]
    public async Task TanimaHatasi_DinlemeDurur_KullaniciyaMesajGider()
    {
        var mikrofon = new SahteMikrofon();
        var gunlukcu = new SahteGunlukcu();
        var yonetici = new TanimaYoneticisi(mikrofon, new SahteTanici { HataVersin = true }, new SahteSaat(), gunlukcu);
        var hatalar = new List<string>();
        yonetici.HataOldu += hatalar.Add;

        yonetici.Baslat(0);
        Gonder(mikrofon, IkiCumle);
        await yonetici.DurdurAsync();

        CollectionAssert.AreEqual(new[] { "Model dosyası bulunamadı." }, hatalar);
        Assert.AreEqual(TanimaDurumu.Hazir, yonetici.Durum);
        Assert.IsFalse(mikrofon.Acik);
        Assert.IsTrue(gunlukcu.Satirlar.Any(s => s.StartsWith("HATA")));
    }

    [TestMethod]
    public async Task MikrofonCikarilirsa_DinlemeDurur_KullaniciyaMesajGider()
    {
        var mikrofon = new SahteMikrofon();
        var yonetici = new TanimaYoneticisi(mikrofon, new SahteTanici(), new SahteSaat(), new SahteGunlukcu());
        var bitti = new TaskCompletionSource<string>();
        yonetici.HataOldu += m => bitti.TrySetResult(m);

        yonetici.Baslat(0);
        mikrofon.Kes();
        string mesaj = await bitti.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await yonetici.DurdurAsync();

        StringAssert.Contains(mesaj, "Mikrofon");
        Assert.AreEqual(TanimaDurumu.Hazir, yonetici.Durum);
    }

    [TestMethod]
    public async Task Temizle_SonuclariSiler_YenidenBaslatilabilir()
    {
        var mikrofon = new SahteMikrofon();
        var yonetici = new TanimaYoneticisi(mikrofon, new SahteTanici("bir", "iki", "üç"), new SahteSaat(), new SahteGunlukcu());
        bool temizlendi = false;
        yonetici.Temizlendi += () => temizlendi = true;

        yonetici.Baslat(0);
        Gonder(mikrofon, IkiCumle);
        await yonetici.DurdurAsync();
        yonetici.Temizle();

        Assert.IsTrue(temizlendi);
        Assert.IsEmpty(yonetici.Sonuclar);

        yonetici.Baslat(0);
        Gonder(mikrofon, SesUretici.Konusma(800));
        await yonetici.DurdurAsync();
        Assert.AreEqual("üç", yonetici.Sonuclar.Single().Metin);
    }

    [TestMethod]
    public void IkinciKezBaslat_BugdurVeFirlatir()
    {
        var yonetici = new TanimaYoneticisi(new SahteMikrofon(), new SahteTanici(), new SahteSaat(), new SahteGunlukcu());
        yonetici.Baslat(0);

        Assert.ThrowsExactly<InvalidOperationException>(() => yonetici.Baslat(0));
    }
}
