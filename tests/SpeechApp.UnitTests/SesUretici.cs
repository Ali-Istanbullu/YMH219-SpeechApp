namespace SpeechApp.UnitTests;

// Testler icin yapay ses: "konusma" yerine 220 Hz sinus (siddeti esigin cok ustu), "sessizlik" yerine sifir.
public static class SesUretici
{
    public const int OrnekHizi = 16000;

    public static float[] Konusma(int ms) =>
        Enumerable.Range(0, ms * OrnekHizi / 1000).Select(i => 0.3f * MathF.Sin(2 * MathF.PI * 220 * i / OrnekHizi)).ToArray();

    public static float[] Sessizlik(int ms) => new float[ms * OrnekHizi / 1000];

    public static float[] Birlestir(params float[][] parcalar) => parcalar.SelectMany(p => p).ToArray();

    // Mikrofon gibi kucuk bloklar halinde (100 ms) verir.
    public static IEnumerable<float[]> Bloklar(float[] ses, int blokOrnek = 1600)
    {
        for (int i = 0; i < ses.Length; i += blokOrnek)
        {
            yield return ses[i..Math.Min(i + blokOrnek, ses.Length)];
        }
    }
}
