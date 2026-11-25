using SpeechApp.Core.Abstractions;

namespace SpeechApp.Infrastructure.Platform;

// Bilgisayarin yerel saati (tanima sonuclarinin zaman damgasi).
public sealed class SistemSaati : ISaat
{
    public DateTimeOffset Simdi => DateTimeOffset.Now;
}
