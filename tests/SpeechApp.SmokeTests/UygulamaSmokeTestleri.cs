using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SpeechApp.Infrastructure.Platform;
using SpeechApp.WinForms.Forms;
using SpeechAppProgram = SpeechApp.WinForms.Program;

namespace SpeechApp.SmokeTests;

[TestClass]
[TestCategory("Smoke")]
public sealed class UygulamaSmokeTestleri
{
    private string _klasor = null!;

    [TestInitialize]
    public void Hazirla()
    {
        _klasor = Path.Combine(Path.GetTempPath(), "speechapp-smoke-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_klasor);
    }

    [TestCleanup]
    public void Temizle()
    {
        try { Directory.Delete(_klasor, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    // SpeechApp.exe'deki GERCEK DI kayitlari eksiksiz mi (ValidateOnBuild), pencere olusuyor mu? Model gerekmez.
    [TestMethod]
    public void DI_GercekKayitlarlaKurulur()
    {
        using ServiceProvider servisler = SpeechAppProgram.ServisleriKur(new BosGunlukcu(), Path.Combine(_klasor, "yok.bin"));

        using AnaForm form = servisler.GetRequiredService<AnaForm>();
        Assert.IsNotNull(form);
    }

    // Exe acilir, kapat denince duzgun kapanir. Mikrofon/Turkce ses/model olmasa da acilista mesaj kutusu cikmaz.
    [TestMethod]
    public void Exe_AcilirVeDuzgunKapanir()
    {
        string exe = Path.Combine(AppContext.BaseDirectory, "SpeechApp.exe");
        if (!File.Exists(exe))
        {
            Assert.Inconclusive($"Exe bulunamadi: {exe}");
        }

        var bilgi = new ProcessStartInfo(exe) { UseShellExecute = false };
        bilgi.Environment[UygulamaYollari.TestKlasoruDegiskeni] = _klasor;
        bilgi.Environment[UygulamaYollari.ModelDegiskeni] = Path.Combine(_klasor, "yok.bin");
        using Process surec = Process.Start(bilgi)!;
        try
        {
            var sayac = Stopwatch.StartNew();
            while (surec.MainWindowHandle == IntPtr.Zero && !surec.HasExited && sayac.ElapsedMilliseconds < 15000)
            {
                Thread.Sleep(100);
                surec.Refresh();
            }
            Assert.IsFalse(surec.HasExited, $"SpeechApp acilir acilmaz kapandi. Log: {_klasor}\\logs");
            Assert.AreNotEqual(IntPtr.Zero, surec.MainWindowHandle, "Ana pencere 15 sn icinde acilmadi.");
            Thread.Sleep(1500);   // acilis isleri (ses ve mikrofon listesi) bitsin

            surec.Refresh();
            StringAssert.StartsWith(surec.MainWindowTitle, "SpeechApp", "ana pencere yerine bir mesaj kutusu acik olmamali");
            Assert.IsTrue(surec.CloseMainWindow(), "Kapat mesaji gonderilemedi.");
            Assert.IsTrue(surec.WaitForExit(15000), "SpeechApp 15 sn icinde kapanmadi.");
            Assert.AreEqual(0, surec.ExitCode);
            Assert.IsTrue(File.Exists(Path.Combine(_klasor, "logs", "speechapp.log")), "log gecici klasore yazilmali");
        }
        finally
        {
            if (!surec.HasExited)
            {
                surec.Kill();
            }
        }
    }
}
