namespace SpeechApp.Core.Abstractions;

// WAV icerigini hoparlorden calan oynatici (gercegi: NAudio).
// SOZLESME: CalmaBitti, Cal'i cagiran thread'in baglaminda (WinForms'ta arayuz thread'i) ve
// sadece ses KENDILIGINDEN bitince tetiklenir; Durdur ile kesilince tetiklenmez.
public interface ISesCalar
{
    event Action? CalmaBitti;

    void Cal(byte[] wav);   // ses cihazi yoksa SeslendirmeHatasi
    void Duraklat();
    void Devam();
    void Durdur();
}
