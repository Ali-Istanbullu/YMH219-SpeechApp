using SpeechApp.Core.Abstractions;
using SpeechApp.Core.Hatalar;

namespace SpeechApp.UnitTests.Fakes;

public sealed class SahteSeslendirici : ISeslendirici
{
    public List<SesBilgisi> Sesler { get; } = [new SesBilgisi("tolga", "Microsoft Tolga")];
    public bool HataVersin { get; set; }

    // Verilirse sentez bu gorev bitene kadar bekler (hazirlanirken Durdur testi icin).
    public TaskCompletionSource? Bekletici { get; set; }

    public (string Metin, string Ses, double Hiz)? SonIstek { get; private set; }

    public IReadOnlyList<SesBilgisi> TurkceSesler() => Sesler;

    public async Task<byte[]> SentezleAsync(string metin, string sesKimligi, double hiz, CancellationToken iptal)
    {
        SonIstek = (metin, sesKimligi, hiz);
        if (Bekletici is { } bekle)
        {
            await bekle.Task.WaitAsync(iptal);
        }
        if (HataVersin)
        {
            throw new SeslendirmeHatasi("Seslendirme motoru çalışmadı.");
        }
        return [1, 2, 3];
    }
}
