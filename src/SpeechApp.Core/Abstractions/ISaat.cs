namespace SpeechApp.Core.Abstractions;

// Simdiki yerel zaman. Tanima sonuclarinin zaman damgasi bundan alinir; testte sabit saat verilir.
public interface ISaat
{
    DateTimeOffset Simdi { get; }
}
