namespace SpeechApp.Core.Abstractions;

// Bir ses parcasini metne ceviren motor (gercegi: Whisper.net, Turkce).
public interface IKonusmaTanici
{
    // Modeli belleğe yukler (ilk seferde bir-iki saniye; sonra hemen doner). Model yok/bozuksa TanimaHatasi.
    // Dinleme baslamadan cagrilir: hata, kullanici konusmaya baslamadan ekrana gelsin.
    Task HazirlaAsync(CancellationToken iptal);

    // ses: 16 kHz tek kanal float. Konusma yoksa bos metin doner. Model/motor hatasinda TanimaHatasi.
    Task<string> CevirAsync(float[] ses, CancellationToken iptal);
}
