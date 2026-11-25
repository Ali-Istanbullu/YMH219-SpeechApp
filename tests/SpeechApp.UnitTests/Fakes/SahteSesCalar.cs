using SpeechApp.Core.Abstractions;

namespace SpeechApp.UnitTests.Fakes;

// Hoparlor yerine: hangi komutun geldigini kaydeder; testi "ses bitti" diyebilir.
public sealed class SahteSesCalar : ISesCalar
{
    public event Action? CalmaBitti;

    public List<string> Komutlar { get; } = [];

    public void Cal(byte[] wav) => Komutlar.Add($"cal:{wav.Length}");
    public void Duraklat() => Komutlar.Add("duraklat");
    public void Devam() => Komutlar.Add("devam");
    public void Durdur() => Komutlar.Add("durdur");

    public void Bitir() => CalmaBitti?.Invoke();
}
