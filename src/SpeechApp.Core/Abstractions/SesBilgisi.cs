namespace SpeechApp.Core.Abstractions;

// Bilgisayarda yuklu bir seslendirme sesi. Kimlik: Windows'un ses kimligi; Ad: ekranda gorunen ad (orn. "Microsoft Tolga").
public sealed record SesBilgisi(string Kimlik, string Ad);
