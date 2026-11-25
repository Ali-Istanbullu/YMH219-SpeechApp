namespace SpeechApp.Core.Tanima;

// Mikrofondan gelen kesintisiz sesi, konusmadaki sessizliklerden PARCALARA boler.
// Her parca ayri ayri metne cevrilir; boylece konustukca metin gelir (kayit bitmesini beklemeden).
//
// Nasil: ses 20 ms'lik cercevelere ayrilir, her cercevenin ses siddeti (RMS: orneklerin karelerinin ortalamasinin
// karekoku) olculur. Esigin ustu "konusma", alti "sessizlik" sayilir.
//  - Konusmadan sonra SessizlikMs kadar sessizlik gelince parca biter.
//  - Toplam konusmasi EnKisaKonusmaMs'den kisa parca atilir (tik, oksuruk, masa sesi).
//  - Parca EnUzunParcaMs'i gecerse zorla kesilir (Whisper tek seferde en fazla 30 sn dinler).
//  - Konusma baslamadan onceki OnSesMs kadar ses parcaya eklenir: kelimenin ilk hecesi kesilmesin.
// Tek thread'den kullanilir.
public sealed class SessizlikAyirici
{
    public const int OrnekHizi = 16000;
    private const int CerceveMs = 20;
    private const int CerceveOrnek = OrnekHizi * CerceveMs / 1000;   // 320

    private readonly float _esik;
    private readonly int _sessizlikCerceve;
    private readonly int _enKisaKonusmaCerceve;
    private readonly int _enUzunParcaOrnek;
    private readonly int _onSesCerceve;

    private readonly float[] _cerceve = new float[CerceveOrnek];
    private int _cerceveDolu;
    private readonly Queue<float[]> _onSes = new();   // konusma baslamadan onceki son birkac cerceve
    private readonly List<float> _parca = [];
    private bool _konusmaVar;
    private int _konusmaCerceve;
    private int _artArdaSessizCerceve;

    public SessizlikAyirici(float esik = 0.015f, int sessizlikMs = 700, int enKisaKonusmaMs = 300,
        int enUzunParcaMs = 25_000, int onSesMs = 300)
    {
        _esik = esik;
        _sessizlikCerceve = sessizlikMs / CerceveMs;
        _enKisaKonusmaCerceve = enKisaKonusmaMs / CerceveMs;
        _enUzunParcaOrnek = enUzunParcaMs * OrnekHizi / 1000;
        _onSesCerceve = onSesMs / CerceveMs;
    }

    // Yeni sesi ekler; bu ekleme ile tamamlanan parcalari (varsa) dondurur.
    public List<float[]> Ekle(ReadOnlySpan<float> ses)
    {
        var bitenler = new List<float[]>();
        while (!ses.IsEmpty)
        {
            int alinan = Math.Min(CerceveOrnek - _cerceveDolu, ses.Length);
            ses[..alinan].CopyTo(_cerceve.AsSpan(_cerceveDolu));
            _cerceveDolu += alinan;
            ses = ses[alinan..];
            if (_cerceveDolu == CerceveOrnek)
            {
                CerceveIsle(bitenler);
                _cerceveDolu = 0;
            }
        }
        return bitenler;
    }

    // Kayit bitti: elde yarim kalan parca varsa (ve yeterince konusma iceriyorsa) onu dondurur.
    public float[]? Bitir()
    {
        if (_konusmaVar && _cerceveDolu > 0)
        {
            _parca.AddRange(_cerceve.AsSpan(0, _cerceveDolu));
        }
        _cerceveDolu = 0;
        float[]? son = _konusmaVar ? ParcayiKapat() : null;
        _onSes.Clear();
        return son;
    }

    private void CerceveIsle(List<float[]> bitenler)
    {
        bool konusma = Siddet(_cerceve) > _esik;
        if (!_konusmaVar)
        {
            if (!konusma)
            {
                _onSes.Enqueue((float[])_cerceve.Clone());
                if (_onSes.Count > _onSesCerceve)
                {
                    _onSes.Dequeue();
                }
                return;
            }
            // Konusma basladi: once onceki sessiz kismi (on ses) ekle.
            _konusmaVar = true;
            foreach (float[] onceki in _onSes)
            {
                _parca.AddRange(onceki);
            }
            _onSes.Clear();
        }

        _parca.AddRange(_cerceve);
        if (konusma)
        {
            _konusmaCerceve++;
            _artArdaSessizCerceve = 0;
        }
        else
        {
            _artArdaSessizCerceve++;
        }

        if (_artArdaSessizCerceve >= _sessizlikCerceve || _parca.Count >= _enUzunParcaOrnek)
        {
            if (ParcayiKapat() is { } parca)
            {
                bitenler.Add(parca);
            }
        }
    }

    // Parcayi bitirir; yeterince konusma yoksa (sadece gurultu) null.
    private float[]? ParcayiKapat()
    {
        float[]? sonuc = _konusmaCerceve >= _enKisaKonusmaCerceve ? [.. _parca] : null;
        _parca.Clear();
        _konusmaVar = false;
        _konusmaCerceve = 0;
        _artArdaSessizCerceve = 0;
        return sonuc;
    }

    private static float Siddet(ReadOnlySpan<float> cerceve)
    {
        double toplam = 0;
        foreach (float ornek in cerceve)
        {
            toplam += ornek * ornek;
        }
        return (float)Math.Sqrt(toplam / cerceve.Length);
    }
}
