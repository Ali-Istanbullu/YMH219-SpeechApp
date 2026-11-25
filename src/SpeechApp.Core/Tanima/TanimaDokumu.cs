using System.Globalization;
using System.Text;

namespace SpeechApp.Core.Tanima;

// Tanınan metni .txt dosyasina yazilacak bicime cevirir (O2): her satir "[SS:dd:ss] metin".
public static class TanimaDokumu
{
    public static string Olustur(IEnumerable<TanimaSonucu> sonuclar)
    {
        var metin = new StringBuilder();
        foreach (TanimaSonucu sonuc in sonuclar)
        {
            metin.Append('[').Append(sonuc.Zaman.ToString("HH:mm:ss", CultureInfo.InvariantCulture)).Append("] ")
                 .Append(sonuc.Metin).Append("\r\n");
        }
        return metin.ToString();
    }
}
