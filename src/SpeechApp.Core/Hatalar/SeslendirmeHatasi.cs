namespace SpeechApp.Core.Hatalar;

// BEKLENEN hata: Metin seslendirilemedi (Turkce ses yok, ses cihazi yok ya da motor hatasi).
// Message kullaniciya aynen gosterilir, bu yuzden Turkce ve ne yapilacagini soyler.
public sealed class SeslendirmeHatasi : Exception
{
    public SeslendirmeHatasi(string mesaj) : base(mesaj)
    {
    }

    public SeslendirmeHatasi(string mesaj, Exception ic) : base(mesaj, ic)
    {
    }
}
