using SpeechApp.Core.Abstractions;

namespace SpeechApp.WinForms.Platform;

// Uygulamanin "son savunma hatti": kodumuzdaki (bug) ve gozden kacan hatalar buraya duser.
// DI'dan gelmez: DI kurulmadan ONCE devreye girmeli. Gunlukcu hazir olunca GunlukcuyuAta ile verilir.
public sealed class GenelHataYakalayici
{
    public const string Baslik = "SpeechApp";
    private const string GenelMesaj =
        "Beklenmeyen bir hata oluştu. Ayrıntı log dosyasına yazıldı.\n" +
        "Sorun sürerse SpeechApp'i kapatıp yeniden açın.";

    private IGunlukcu? _gunlukcu;

    // Mesaj kutusu en fazla bir kez acilir; sonrakiler sadece loga gider.
    private bool _ekrandaGosterildi;

    public void Kur()
    {
        // Arka plan is parcaciklarindaki yakalanmamis hata en azindan loga yazilsin.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Bildir(e.ExceptionObject as Exception ?? new InvalidOperationException("Bilinmeyen hata"));

        if (DerlemeModu.HataAyiklamaMi)
        {
            // Debug: arayuz hatalari YAKALANMAZ, debugger hatanin oldugu satirda durur.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            return;
        }

        // Release: arayuz is parcacigindaki hata uygulamayi kapatmaz; loga yazilir, kullanici bilgilendirilir.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Bildir(e.Exception);

        // await edilmemis Task hatalari: loga yaz ve "goruldu" isaretle.
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Bildir(e.Exception);
            e.SetObserved();
        };
    }

    public void GunlukcuyuAta(IGunlukcu gunlukcu)
    {
        ArgumentNullException.ThrowIfNull(gunlukcu);
        _gunlukcu = gunlukcu;
    }

    private void Bildir(Exception hata)
    {
        try
        {
            _gunlukcu?.Hata(hata, "Yakalanmamis hata (bug).");
            if (!_ekrandaGosterildi)
            {
                _ekrandaGosterildi = true;
                MessageBox.Show(GenelMesaj, Baslik, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch
        {
            // son savunma hattinda da exception disari cikmaz.
        }
    }
}
