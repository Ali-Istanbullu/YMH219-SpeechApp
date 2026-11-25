using SpeechApp.Core.Hatalar;
using SpeechApp.Core.Seslendirme;
using SpeechApp.UnitTests.Fakes;

namespace SpeechApp.UnitTests;

[TestClass]
public sealed class SeslendirmeYoneticisiTestleri
{
    private readonly SahteSeslendirici _seslendirici = new();
    private readonly SahteSesCalar _calar = new();
    private SeslendirmeYoneticisi Yeni() => new(_seslendirici, _calar, new SahteGunlukcu());

    [TestMethod]
    public async Task Baslat_MetniSeslendirirVeCalar_BitinceHazir()
    {
        SeslendirmeYoneticisi yonetici = Yeni();
        var durumlar = new List<SeslendirmeDurumu>();
        yonetici.DurumDegisti += () => durumlar.Add(yonetici.Durum);

        await yonetici.BaslatAsync("  Merhaba dünya  ", "tolga", 1.5);
        Assert.AreEqual(("Merhaba dünya", "tolga", 1.5), _seslendirici.SonIstek);
        CollectionAssert.AreEqual(new[] { "cal:3" }, _calar.Komutlar);
        Assert.AreEqual(SeslendirmeDurumu.Okunuyor, yonetici.Durum);

        _calar.Bitir();

        CollectionAssert.AreEqual(
            new[] { SeslendirmeDurumu.Hazirlaniyor, SeslendirmeDurumu.Okunuyor, SeslendirmeDurumu.Hazir }, durumlar);
    }

    [TestMethod]
    public async Task DuraklatDevamDurdur()
    {
        SeslendirmeYoneticisi yonetici = Yeni();
        await yonetici.BaslatAsync("metin", "tolga", 1);

        yonetici.DuraklatVeyaDevamEt();
        Assert.AreEqual(SeslendirmeDurumu.Duraklatildi, yonetici.Durum);
        yonetici.DuraklatVeyaDevamEt();
        Assert.AreEqual(SeslendirmeDurumu.Okunuyor, yonetici.Durum);
        yonetici.Durdur();
        Assert.AreEqual(SeslendirmeDurumu.Hazir, yonetici.Durum);
        yonetici.Durdur();   // Hazir'dayken bir sey yapmaz

        CollectionAssert.AreEqual(new[] { "cal:3", "duraklat", "devam", "durdur" }, _calar.Komutlar);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   \r\n ")]
    public async Task BosMetin_AnlasilirHata(string metin)
    {
        SeslendirmeYoneticisi yonetici = Yeni();

        SeslendirmeHatasi hata = await Assert.ThrowsExactlyAsync<SeslendirmeHatasi>(() => yonetici.BaslatAsync(metin, "tolga", 1));

        StringAssert.Contains(hata.Message, "metin yok");
        Assert.AreEqual(SeslendirmeDurumu.Hazir, yonetici.Durum);
    }

    [TestMethod]
    public async Task MotorHatasi_HazirDurumunaDoner_HataCagiranaGider()
    {
        _seslendirici.HataVersin = true;
        SeslendirmeYoneticisi yonetici = Yeni();

        await Assert.ThrowsExactlyAsync<SeslendirmeHatasi>(() => yonetici.BaslatAsync("metin", "tolga", 1));

        Assert.AreEqual(SeslendirmeDurumu.Hazir, yonetici.Durum);
        Assert.IsEmpty(_calar.Komutlar);
    }

    [TestMethod]
    public async Task HazirlanirkenDurdur_HicCalmaz()
    {
        _seslendirici.Bekletici = new TaskCompletionSource();
        SeslendirmeYoneticisi yonetici = Yeni();

        Task okuma = yonetici.BaslatAsync("uzun metin", "tolga", 1);
        Assert.AreEqual(SeslendirmeDurumu.Hazirlaniyor, yonetici.Durum);
        yonetici.Durdur();
        await okuma;

        Assert.AreEqual(SeslendirmeDurumu.Hazir, yonetici.Durum);
        Assert.IsEmpty(_calar.Komutlar);
    }

    [TestMethod]
    public void HizSinirlarDisindaysa_SiniraCekilir()
    {
        SeslendirmeYoneticisi yonetici = Yeni();

        _ = yonetici.BaslatAsync("a", "tolga", 9);

        Assert.AreEqual(SeslendirmeYoneticisi.EnYuksekHiz, _seslendirici.SonIstek!.Value.Hiz);
    }

    [TestMethod]
    public void TurkceSesYoksa_NeredenEkleneceginiSoyler()
    {
        _seslendirici.Sesler.Clear();
        SeslendirmeYoneticisi yonetici = Yeni();

        SeslendirmeHatasi hata = Assert.ThrowsExactly<SeslendirmeHatasi>(() => yonetici.SesleriGetir());

        StringAssert.Contains(hata.Message, "Ses ekle");
    }
}
