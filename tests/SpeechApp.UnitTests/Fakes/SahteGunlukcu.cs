using SpeechApp.Core.Abstractions;

namespace SpeechApp.UnitTests.Fakes;

// Loglari bellekte tutar; testler "su yazildi mi" diye bakabilir.
public sealed class SahteGunlukcu : IGunlukcu
{
    public List<string> Satirlar { get; } = [];

    public void Bilgi(string mesaj) => Ekle("BILGI " + mesaj);
    public void Uyari(string mesaj) => Ekle("UYARI " + mesaj);
    public void Hata(Exception hata, string mesaj) => Ekle("HATA " + mesaj + " | " + hata.Message);

    private void Ekle(string satir)
    {
        lock (Satirlar)
        {
            Satirlar.Add(satir);
        }
    }
}
