using SpeechApp.Core.Tanima;

namespace SpeechApp.UnitTests;

[TestClass]
public sealed class TanimaDokumuTestleri
{
    [TestMethod]
    public void HerSonucBirSatir_ZamanDamgali()
    {
        var saat = new DateTimeOffset(2026, 11, 25, 9, 5, 7, TimeSpan.FromHours(3));
        TanimaSonucu[] sonuclar = [new(saat, "Günaydın"), new(saat.AddSeconds(65), "Çay içelim mi?")];

        Assert.AreEqual("[09:05:07] Günaydın\r\n[09:06:12] Çay içelim mi?\r\n", TanimaDokumu.Olustur(sonuclar));
        Assert.AreEqual("", TanimaDokumu.Olustur([]));
    }
}
