namespace SpeechApp.Core.Abstractions;

// Metni sese ceviren motor (gercegi: Windows.Media.SpeechSynthesis).
public interface ISeslendirici
{
    // Yuklu Turkce sesler. Hic yoksa bos liste.
    IReadOnlyList<SesBilgisi> TurkceSesler();

    // Metni WAV dosyasi icerigine cevirir. hiz: 1 = normal, 0,5 = yarisi, 2 = iki kati.
    // Motor hatasinda SeslendirmeHatasi firlatir.
    Task<byte[]> SentezleAsync(string metin, string sesKimligi, double hiz, CancellationToken iptal);
}
