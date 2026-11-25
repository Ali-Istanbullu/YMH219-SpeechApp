namespace SpeechApp.Core.Hatalar;

// BEKLENEN hata: Mikrofona erisilemedi (yok, izin kapali ya da baska uygulama kullaniyor).
// Message kullaniciya aynen gosterilir, bu yuzden Turkce ve ne yapilacagini soyler.
public sealed class MikrofonHatasi : Exception
{
    public MikrofonHatasi(string mesaj) : base(mesaj)
    {
    }

    public MikrofonHatasi(string mesaj, Exception ic) : base(mesaj, ic)
    {
    }
}
