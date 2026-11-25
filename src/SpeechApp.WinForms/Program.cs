using Microsoft.Extensions.DependencyInjection;
using SpeechApp.Core.Abstractions;
using SpeechApp.Core.Seslendirme;
using SpeechApp.Core.Tanima;
using SpeechApp.Infrastructure.Platform;
using SpeechApp.Infrastructure.Seslendirme;
using SpeechApp.Infrastructure.Tanima;
using SpeechApp.WinForms.Forms;
using SpeechApp.WinForms.Platform;

namespace SpeechApp.WinForms;

// ================================================================================
//  SPEECHAPP BASLANGIC NOKTASI ve "KOMPOZISYON KOKU"
//   1) Genel hata yakalayici ve log
//   2) Siniflar birbirine baglanir: DI container SADECE burada kurulur
//   3) Pencere acilir; kapaninca mikrofon, hoparlor ve model duzgun kapatilir
// ================================================================================
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var genelHataYakalayici = new GenelHataYakalayici();
        genelHataYakalayici.Kur();

        ApplicationConfiguration.Initialize();

        using var gunlukcu = new SerilogGunlukcu(LogDosyasi());
        genelHataYakalayici.GunlukcuyuAta(gunlukcu);
        gunlukcu.Bilgi($"SpeechApp {Application.ProductVersion} açıldı.");

        try
        {
            using ServiceProvider servisler = ServisleriKur(gunlukcu, UygulamaYollari.ModelDosyasi());
            Application.Run(servisler.GetRequiredService<AnaForm>());
        }
        catch (Exception hata) when (!DerlemeModu.HataAyiklamaMi)   // Debug'da debugger dursun
        {
            gunlukcu.Hata(hata, "SpeechApp baslatilamadi.");
            MessageBox.Show("SpeechApp başlatılamadı. Ayrıntı log dosyasında.", GenelHataYakalayici.Baslik,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            gunlukcu.Bilgi("SpeechApp kapandı.");
        }
    }

    // ================= DI KURULUMU (hepsi Singleton: tek pencere, tek mikrofon, tek model) =================
    // internal: smoke testleri ayni kayitlarla container'i kurabilsin.
    internal static ServiceProvider ServisleriKur(IGunlukcu gunlukcu, string modelYolu)
    {
        var servisler = new ServiceCollection();

        servisler.AddSingleton(gunlukcu);
        servisler.AddSingleton<ISaat, SistemSaati>();

        // ---- Metni seslendir ----
        servisler.AddSingleton<ISeslendirici, WindowsSeslendirici>();
        servisler.AddSingleton<ISesCalar, NAudioSesCalar>();
        servisler.AddSingleton<SeslendirmeYoneticisi>();

        // ---- Konusmayi metne cevir ----
        servisler.AddSingleton<IMikrofon, NAudioMikrofon>();
        servisler.AddSingleton<IKonusmaTanici>(_ => new WhisperTanici(modelYolu, gunlukcu));
        servisler.AddSingleton<TanimaYoneticisi>();

        // ---- Pencere ----
        servisler.AddSingleton<AnaForm>();

        return servisler.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    private static string LogDosyasi()
    {
        try
        {
            return UygulamaYollari.GunlukDosyasi();
        }
        catch (Exception hata) when (hata is IOException or UnauthorizedAccessException)
        {
            return Path.Combine(Path.GetTempPath(), "SpeechApp", "speechapp.log");
        }
    }
}
