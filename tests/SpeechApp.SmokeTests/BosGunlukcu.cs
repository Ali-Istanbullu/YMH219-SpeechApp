using SpeechApp.Core.Abstractions;

namespace SpeechApp.SmokeTests;

public sealed class BosGunlukcu : IGunlukcu
{
    public List<string> Satirlar { get; } = [];

    public void Bilgi(string mesaj) => Satirlar.Add(mesaj);
    public void Uyari(string mesaj) => Satirlar.Add(mesaj);
    public void Hata(Exception hata, string mesaj) => Satirlar.Add(mesaj + " | " + hata.Message);
}
