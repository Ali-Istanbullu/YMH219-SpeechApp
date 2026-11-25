namespace SpeechApp.Core.Tanima;

// Tanınan bir parca: ne zaman soylendigi (parcanin bittigi an) ve metni. Zaman damgali listenin bir satiri (O3).
public sealed record TanimaSonucu(DateTimeOffset Zaman, string Metin);
