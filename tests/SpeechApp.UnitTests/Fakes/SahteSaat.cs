using SpeechApp.Core.Abstractions;

namespace SpeechApp.UnitTests.Fakes;

// Her okumada 1 saniye ilerleyen saat: sonuclarin zaman damgalari tahmin edilebilir olur.
public sealed class SahteSaat : ISaat
{
    private DateTimeOffset _simdi = new(2026, 11, 25, 14, 3, 20, TimeSpan.FromHours(3));

    public DateTimeOffset Simdi
    {
        get
        {
            DateTimeOffset deger = _simdi;
            _simdi = _simdi.AddSeconds(1);
            return deger;
        }
    }
}
