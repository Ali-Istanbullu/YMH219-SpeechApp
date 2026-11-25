using SpeechApp.Core.Abstractions;
using SpeechApp.Core.Hatalar;

namespace SpeechApp.UnitTests.Fakes;

// Her parca icin sirayla verilen metni dondurur; istenirse hata firlatir.
public sealed class SahteTanici : IKonusmaTanici
{
    private readonly Queue<string> _cevaplar;

    public SahteTanici(params string[] cevaplar) => _cevaplar = new Queue<string>(cevaplar);

    public List<int> GelenParcaUzunluklari { get; } = [];
    public bool HataVersin { get; set; }

    public async Task<string> CevirAsync(float[] ses, CancellationToken iptal)
    {
        await Task.Yield();   // gercek tanici gibi baska thread'de bitsin
        if (HataVersin)
        {
            throw new TanimaHatasi("Model dosyası bulunamadı.");
        }
        GelenParcaUzunluklari.Add(ses.Length);
        return _cevaplar.Count > 0 ? _cevaplar.Dequeue() : "";
    }
}
