namespace SpeechApp.Core.Tanima;

// Ekranda gosterilen konusma tanima durumu (G5).
public enum TanimaDurumu
{
    Hazir,         // mikrofon kapali
    Hazirlaniyor,  // konusma tanima modeli yukleniyor (ilk Baslat'ta)
    Dinleniyor,    // mikrofon acik; sessizlik geldikce parcalar cevriliyor
    Bitiriliyor    // Durdur'a basildi; elde kalan parcalar cevriliyor
}
