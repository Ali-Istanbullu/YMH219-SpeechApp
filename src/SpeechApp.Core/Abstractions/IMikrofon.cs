namespace SpeechApp.Core.Abstractions;

// Mikrofondan ses alan kaydedici (gercegi: NAudio).
// Ses her zaman 16 kHz, tek kanal, -1..1 arasi float ornekler olarak gelir (Whisper'in istedigi bicim).
// SOZLESME: SesGeldi ve KayitKesildi ARKA PLAN thread'inden gelebilir.
public interface IMikrofon
{
    event Action<float[]>? SesGeldi;

    // Kayit beklenmedik sekilde durdu (mikrofon cikarildi vb.).
    event Action<Exception>? KayitKesildi;

    // Bilgisayardaki mikrofonlarin adlari; sirasi Baslat'taki cihazNo'dur.
    IReadOnlyList<string> Cihazlar();

    // Mikrofon yoksa / izin kapaliysa MikrofonHatasi.
    void Baslat(int cihazNo);

    void Durdur();
}
