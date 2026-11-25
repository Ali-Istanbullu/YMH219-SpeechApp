using System.Threading.Channels;
using SpeechApp.Core.Abstractions;
using SpeechApp.Core.Hatalar;

namespace SpeechApp.Core.Tanima;

// "Konusmayi Metne Cevir" bolumunun mantigi.
// Mikrofon sesi (arka plan thread'i) bir kuyruga yazilir; tek bir dongu kuyrugu okur, SessizlikAyirici ile
// parcalara boler ve parcalari SIRAYLA metne cevirtir. Dongu arayuz thread'inden baslatildigi icin olaylar
// (DurumDegisti, SonucEklendi, HataOldu) arayuz thread'inde gelir.
public sealed class TanimaYoneticisi
{
    private readonly IMikrofon _mikrofon;
    private readonly IKonusmaTanici _tanici;
    private readonly ISaat _saat;
    private readonly IGunlukcu _gunlukcu;
    private readonly List<TanimaSonucu> _sonuclar = [];

    private Channel<float[]>? _kuyruk;
    private TaskCompletionSource? _dongununSonu;

    public TanimaYoneticisi(IMikrofon mikrofon, IKonusmaTanici tanici, ISaat saat, IGunlukcu gunlukcu)
    {
        _mikrofon = mikrofon;
        _tanici = tanici;
        _saat = saat;
        _gunlukcu = gunlukcu;
        _mikrofon.SesGeldi += SesGeldi;
        _mikrofon.KayitKesildi += KayitKesildi;
    }

    public TanimaDurumu Durum { get; private set; } = TanimaDurumu.Hazir;

    // Su an bir parca metne cevriliyor mu (ekranda "Çevriliyor..." icin).
    public bool CeviriSuruyor { get; private set; }

    public IReadOnlyList<TanimaSonucu> Sonuclar => _sonuclar;

    public event Action? DurumDegisti;
    public event Action<TanimaSonucu>? SonucEklendi;
    public event Action? Temizlendi;

    // Beklenen bir hata yuzunden dinleme durdu; mesaj kullaniciya gosterilir.
    public event Action<string>? HataOldu;

    public IReadOnlyList<string> Mikrofonlar() => _mikrofon.Cihazlar();

    // Dinlemeye baslar. Mikrofon acilamazsa MikrofonHatasi cagirana gider, durum Hazir kalir.
    public void Baslat(int mikrofonNo)
    {
        if (Durum != TanimaDurumu.Hazir)
        {
            throw new InvalidOperationException($"Tanima zaten suruyor ({Durum}).");   // bug: buton acik kalmis
        }

        _kuyruk = Channel.CreateUnbounded<float[]>(new UnboundedChannelOptions { SingleReader = true });
        try
        {
            _mikrofon.Baslat(mikrofonNo);
        }
        catch (MikrofonHatasi)
        {
            _kuyruk = null;   // durum Hazir kalir; hata cagirana (ekrana) gider
            throw;
        }
        _dongununSonu = new TaskCompletionSource();
        DurumYap(TanimaDurumu.Dinleniyor);
        _gunlukcu.Bilgi($"Dinleme basladi (mikrofon {mikrofonNo}).");
        DonguyuCalistir(_kuyruk, _dongununSonu);
    }

    // Mikrofonu kapatir; elde kalan parcalar da cevrilip bitince doner.
    public async Task DurdurAsync()
    {
        if (Durum != TanimaDurumu.Dinleniyor)
        {
            if (_dongununSonu is { } bekleyen)
            {
                await bekleyen.Task;   // zaten bitiriliyor: onu bekle
            }
            return;
        }
        _mikrofon.Durdur();
        DurumYap(TanimaDurumu.Bitiriliyor);
        _kuyruk!.Writer.TryComplete();
        await _dongununSonu!.Task;
    }

    // Ekrandaki tanima sonuclarini siler (G2 Temizle). Dinleme suruyorsa devam eder.
    public void Temizle()
    {
        _sonuclar.Clear();
        Temizlendi?.Invoke();
    }

    // ARKA PLAN thread'i: sadece kuyruga yazar.
    private void SesGeldi(float[] ses) => _kuyruk?.Writer.TryWrite(ses);

    // ARKA PLAN thread'i: donguye hatayi iletir, dongu kullaniciya bildirir.
    private void KayitKesildi(Exception hata) =>
        _kuyruk?.Writer.TryComplete(new MikrofonHatasi("Mikrofon kaydı kesildi (mikrofon çıkarılmış olabilir).", hata));

    // async void BILEREK: beklenmeyen bir hata (bug) arayuz thread'ine firlar ve genel hata yakalayiciya gider;
    // gorevin icinde sessizce kaybolmaz. Beklenen hatalar icerde yakalanip HataOldu ile bildirilir.
    private async void DonguyuCalistir(Channel<float[]> kuyruk, TaskCompletionSource son)
    {
        var ayirici = new SessizlikAyirici();
        try
        {
            await foreach (float[] ses in kuyruk.Reader.ReadAllAsync())
            {
                foreach (float[] parca in ayirici.Ekle(ses))
                {
                    await CevirVeEkleAsync(parca);
                }
            }
            if (ayirici.Bitir() is { } kalan)
            {
                await CevirVeEkleAsync(kalan);
            }
        }
        catch (Exception hata) when (hata is MikrofonHatasi or TanimaHatasi)
        {
            _mikrofon.Durdur();
            kuyruk.Writer.TryComplete();
            _gunlukcu.Hata(hata, "Dinleme hata ile durdu.");
            HataOldu?.Invoke(hata.Message);
        }
        finally
        {
            CeviriSuruyor = false;
            DurumYap(TanimaDurumu.Hazir);
            son.TrySetResult();
        }
    }

    private async Task CevirVeEkleAsync(float[] parca)
    {
        CeviriSuruyor = true;
        DurumDegisti?.Invoke();
        string metin = await _tanici.CevirAsync(parca, CancellationToken.None);
        CeviriSuruyor = false;
        DurumDegisti?.Invoke();

        metin = metin.Trim();
        if (metin.Length == 0)
        {
            return;   // konusma degil gurultuymus
        }
        var sonuc = new TanimaSonucu(_saat.Simdi, metin);
        _sonuclar.Add(sonuc);
        SonucEklendi?.Invoke(sonuc);
    }

    private void DurumYap(TanimaDurumu yeni)
    {
        if (Durum == yeni)
        {
            return;
        }
        Durum = yeni;
        DurumDegisti?.Invoke();
    }
}
