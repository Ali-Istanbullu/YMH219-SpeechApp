namespace SpeechApp.Core.Abstractions;

// Log yazma sozlesmesi. Core, Serilog'u bilmez; uygulamasi Infrastructure'da.
// KURAL: tanınan/okunan metinler ASLA loga yazilmaz (kisinin konustugu sey ozeldir).
public interface IGunlukcu
{
    void Bilgi(string mesaj);
    void Uyari(string mesaj);
    void Hata(Exception hata, string mesaj);
}
