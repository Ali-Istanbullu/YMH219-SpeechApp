namespace SpeechApp.Core.Seslendirme;

// Ekranda gosterilen seslendirme durumu (G5).
public enum SeslendirmeDurumu
{
    Hazir,          // bir sey okunmuyor
    Hazirlaniyor,   // metin sese cevriliyor (uzun metinde bir-iki saniye)
    Okunuyor,
    Duraklatildi
}
