namespace SpeechApp.Core.Hatalar;

// BEKLENEN hata: Konusma tanima calismadi (model dosyasi yok/bozuk ya da motor hatasi).
// Message kullaniciya aynen gosterilir, bu yuzden Turkce ve ne yapilacagini soyler.
public sealed class TanimaHatasi : Exception
{
    public TanimaHatasi(string mesaj) : base(mesaj)
    {
    }

    public TanimaHatasi(string mesaj, Exception ic) : base(mesaj, ic)
    {
    }
}
