using System.Text;
using SpeechApp.Core.Abstractions;
using SpeechApp.Core.Hatalar;
using Whisper.net;
using Whisper.net.LibraryLoader;

namespace SpeechApp.Infrastructure.Tanima;

// Whisper.net (whisper.cpp) ile internetsiz Turkce konusma tanima.
// Model ilk HazirlaAsync'te bir kez belleğe yuklenir (~bir-iki saniye), uygulama kapanana kadar kalir.
// Ceviri CPU'da, arka plan thread'inde yapilir; parcalar zaten sirayla geldigi icin ayni anda tek ceviri olur.
public sealed class WhisperTanici : IKonusmaTanici, IDisposable
{
    // whisper.cpp DLL'lerinin istedigi Visual C++ calisma zamani dosyalari (DLL'lerin import tablosundan).
    private static readonly string[] GerekenVcDosyalari = ["vcruntime140.dll", "vcruntime140_1.dll", "msvcp140.dll", "vcomp140.dll"];

    private readonly string _modelYolu;
    private readonly IGunlukcu _gunlukcu;
    private readonly SemaphoreSlim _kilit = new(1, 1);
    private WhisperFactory? _fabrika;
    private WhisperProcessor? _islemci;

    public WhisperTanici(string modelYolu, IGunlukcu gunlukcu)
    {
        _modelYolu = modelYolu;
        _gunlukcu = gunlukcu;
    }

    public async Task HazirlaAsync(CancellationToken iptal)
    {
        await _kilit.WaitAsync(iptal);
        try
        {
            if (_islemci is null)
            {
                await Task.Run(Yukle, iptal);
            }
        }
        finally
        {
            _kilit.Release();
        }
    }

    public async Task<string> CevirAsync(float[] ses, CancellationToken iptal)
    {
        await HazirlaAsync(iptal);
        await _kilit.WaitAsync(iptal);
        try
        {
            // Olcum: ses suresi ve cevirme suresi loga yazilir (metnin kendisi yazilmaz).
            var sure = System.Diagnostics.Stopwatch.StartNew();
            var metin = new StringBuilder();
            // ProcessAsync isi arka planda yapar; parcalar (segment) geldikce birlestirilir.
            await foreach (SegmentData parca in _islemci!.ProcessAsync(ses, iptal))
            {
                metin.Append(parca.Text);
            }
            double sesSn = ses.Length / 16000.0;
            _gunlukcu.Bilgi($"Parca cevrildi: ses {sesSn:0.0} sn, cevirme {sure.Elapsed.TotalSeconds:0.0} sn " +
                $"(ses suresinin {sure.Elapsed.TotalSeconds / Math.Max(sesSn, 0.1):0.0} kati).");
            return metin.ToString().Trim();
        }
        catch (WhisperProcessingException hata)
        {
            throw new TanimaHatasi("Konuşma metne çevrilemedi (tanıma motoru hatası). Tekrar deneyin.", hata);
        }
        finally
        {
            _kilit.Release();
        }
    }

    public void Dispose()
    {
        _islemci?.Dispose();
        _fabrika?.Dispose();
    }

    private void Yukle()
    {
        if (!File.Exists(_modelYolu))
        {
            throw new TanimaHatasi($"Konuşma tanıma modeli bulunamadı ({Path.GetFileName(_modelYolu)}). " +
                "SpeechApp kurulumunu yeniden çalıştırın; kurulum modeli indirir.");
        }

        string[] eksikler = GerekenVcDosyalari
            .Where(dosya => !File.Exists(Path.Combine(Environment.SystemDirectory, dosya)))
            .ToArray();
        if (eksikler.Length > 0)
        {
            _gunlukcu.Uyari("Eksik Visual C++ dosyalari: " + string.Join(", ", eksikler));
            throw new TanimaHatasi("Konuşma tanıma için gereken Microsoft Visual C++ 2015-2022 (x64) bilgisayarda yok. " +
                "SpeechApp kurulumunu yeniden çalıştırın ya da https://aka.ms/vs/17/release/vc_redist.x64.exe adresinden kurun.");
        }

        // Model dosyasi ilk kullanimda (Build) okunur; bozuk dosya hatasi FromPath'te degil orada gelebilir
        // (smoke testi yakaladi). Ikisi de ayni try icinde.
        WhisperFactory? fabrika = null;
        try
        {
            fabrika = WhisperFactory.FromPath(_modelYolu);
            _islemci = fabrika.CreateBuilder().WithLanguage("tr").Build();
            _fabrika = fabrika;
        }
        catch (WhisperModelLoadException hata)
        {
            fabrika?.Dispose();
            throw new TanimaHatasi("Konuşma tanıma modeli bozuk ya da yarım inmiş. SpeechApp kurulumunu yeniden çalıştırın.", hata);
        }
        _gunlukcu.Bilgi($"Whisper modeli yuklendi: {Path.GetFileName(_modelYolu)}, calisma zamani: {RuntimeOptions.LoadedLibrary}, " +
            $"islemci cekirdegi: {Environment.ProcessorCount}.");
    }
}
