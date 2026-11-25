using SpeechApp.Core.Abstractions;
using SpeechApp.Core.Hatalar;

namespace SpeechApp.UnitTests.Fakes;

// Testin elle "ses gonderdigi" mikrofon.
public sealed class SahteMikrofon : IMikrofon
{
    public event Action<float[]>? SesGeldi;
    public event Action<Exception>? KayitKesildi;

    public bool Acik { get; private set; }
    public bool BaslatHataVersin { get; set; }

    public IReadOnlyList<string> Cihazlar() => ["Sahte mikrofon"];

    public void Baslat(int cihazNo)
    {
        if (BaslatHataVersin)
        {
            throw new MikrofonHatasi("Mikrofon bulunamadı.");
        }
        Acik = true;
    }

    public void Durdur() => Acik = false;

    public void Gonder(float[] ses) => SesGeldi?.Invoke(ses);

    public void Kes() => KayitKesildi?.Invoke(new IOException("cihaz cikarildi"));
}
