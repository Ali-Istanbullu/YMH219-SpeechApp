namespace SpeechApp.Infrastructure.Platform;

// SpeechApp'in dosyalari:
//   {program klasoru}\model\ggml-small-q8_0.bin   Whisper modeli (kurulum indirir; degismez, kaldirinca silinir)
//   %LocalAppData%\SpeechApp\logs\speechapp*.log   log (1 MB x 5); kaldirinca KALIR (Ali karari)
// Klasorler silinirse her istekte yeniden olusturulur.
public static class UygulamaYollari
{
    public const string ModelAdi = "ggml-small-q8_0.bin";

    // Testler exe'yi gecici bir klasorle calistirir; gercek loglara dokunulmaz.
    public const string TestKlasoruDegiskeni = "SPEECHAPP_VERI_KLASORU";

    // Gelistirme/test icin modeli baska yerden vermek (verilmezse program klasorundeki).
    public const string ModelDegiskeni = "SPEECHAPP_MODEL";

    public static string KokKlasor()
    {
        string? test = Environment.GetEnvironmentVariable(TestKlasoruDegiskeni);
        string klasor = !string.IsNullOrWhiteSpace(test)
            ? test
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpeechApp");
        Directory.CreateDirectory(klasor);
        return klasor;
    }

    public static string GunlukDosyasi()
    {
        string klasor = Path.Combine(KokKlasor(), "logs");
        Directory.CreateDirectory(klasor);
        return Path.Combine(klasor, "speechapp.log");
    }

    public static string ModelDosyasi()
    {
        string? ozel = Environment.GetEnvironmentVariable(ModelDegiskeni);
        return !string.IsNullOrWhiteSpace(ozel) ? ozel : Path.Combine(AppContext.BaseDirectory, "model", ModelAdi);
    }
}
