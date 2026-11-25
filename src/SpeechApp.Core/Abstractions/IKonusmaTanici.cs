namespace SpeechApp.Core.Abstractions;

// Bir ses parcasini metne ceviren motor (gercegi: Whisper.net, Turkce).
public interface IKonusmaTanici
{
    // ses: 16 kHz tek kanal float. Konusma yoksa bos metin doner. Model/motor hatasinda TanimaHatasi.
    Task<string> CevirAsync(float[] ses, CancellationToken iptal);
}
