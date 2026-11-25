using System.Text;
using SpeechApp.Core.Abstractions;
using SpeechApp.Core.Hatalar;
using SpeechApp.Core.Seslendirme;
using SpeechApp.Core.Tanima;

namespace SpeechApp.WinForms.Forms;

// Tek pencere, yan yana iki bolum (Ali karari):
//   Solda  "Metni Seslendir"          -> SeslendirmeYoneticisi
//   Sagda  "Konuşmayı Metne Çevir"    -> TanimaYoneticisi
// Pencere sadece gosterir ve butonlari yoneticilere iletir; mantik Core'da.
// Beklenen hatalar (SeslendirmeHatasi, TanimaHatasi, MikrofonHatasi) mesaj kutusuyla gosterilir; digerleri bug'dir.
public sealed class AnaForm : Form
{
    private static readonly Color Gri = Color.DimGray;
    private static readonly Color Turuncu = Color.DarkOrange;
    private static readonly Color Yesil = Color.SeaGreen;
    private static readonly Color Kirmizi = Color.Firebrick;

    private readonly SeslendirmeYoneticisi _seslendirme;
    private readonly TanimaYoneticisi _tanima;
    private readonly IGunlukcu _gunlukcu;

    // ---- Sol: seslendirme ----
    private readonly RichTextBox _okunacakMetin = new() { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11f), AcceptsTab = false };
    private readonly ComboBox _sesKutusu = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly TrackBar _hizCubugu = new() { Minimum = 5, Maximum = 20, Value = 10, TickFrequency = 5, Width = 150, AutoSize = false, Height = 30 };
    private readonly Label _hizYazisi = new() { AutoSize = true, Margin = new Padding(3, 8, 3, 3) };
    private readonly Button _okuBaslat = Buton("▶ Başlat");
    private readonly Button _okuDuraklat = Buton("⏸ Duraklat");
    private readonly Button _okuDurdur = Buton("■ Durdur");
    private readonly Button _okuTemizle = Buton("Temizle");
    private readonly Label _okumaDurumu = DurumEtiketi();
    private bool _sesVar;

    // ---- Sag: konusma tanima ----
    private readonly ComboBox _mikrofonKutusu = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
    private readonly Button _dinleBaslat = Buton("● Dinlemeyi Başlat", 150);
    private readonly Button _dinleDurdur = Buton("■ Durdur");
    private readonly Button _dinleTemizle = Buton("Temizle");
    private readonly Button _kaydet = Buton("Kaydet (.txt)...", 120);
    private readonly Label _tanimaDurumu = DurumEtiketi();
    private readonly RichTextBox _taninanMetin = new() { Dock = DockStyle.Fill, ReadOnly = true, Font = new Font("Segoe UI", 11f), BackColor = SystemColors.Window };
    private readonly ListView _sonucListesi = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HeaderStyle = ColumnHeaderStyle.Nonclickable };
    private bool _mikrofonVar;
    private bool _kapaniyor;

    public AnaForm(SeslendirmeYoneticisi seslendirme, TanimaYoneticisi tanima, IGunlukcu gunlukcu)
    {
        _seslendirme = seslendirme;
        _tanima = tanima;
        _gunlukcu = gunlukcu;

        Text = "SpeechApp — Metin ↔ Konuşma";
        Font = new Font("Segoe UI", 9.75f);
        MinimumSize = new Size(980, 560);
        Size = new Size(1180, 680);
        StartPosition = FormStartPosition.CenterScreen;

        var ikiBolum = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(8) };
        ikiBolum.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        ikiBolum.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        ikiBolum.Controls.Add(SeslendirmeBolumu(), 0, 0);
        ikiBolum.Controls.Add(TanimaBolumu(), 1, 0);
        Controls.Add(ikiBolum);

        _seslendirme.DurumDegisti += SeslendirmeDurumunuGoster;
        _tanima.DurumDegisti += TanimaDurumunuGoster;
        _tanima.SonucEklendi += SonucuGoster;
        _tanima.Temizlendi += () => { _taninanMetin.Clear(); _sonucListesi.Items.Clear(); ButonlariAyarla(); };
        _tanima.HataOldu += mesaj => HataGoster(mesaj);

        Shown += (_, _) => CihazlariYukle();
        FormClosing += KapanirkenDurdur;
    }

    // ============================ Yerlesim ============================

    private GroupBox SeslendirmeBolumu()
    {
        _hizCubugu.ValueChanged += (_, _) => _hizYazisi.Text = $"{Hiz:0.0}x";
        _hizYazisi.Text = $"{Hiz:0.0}x";

        var ayarlar = Satir(Etiket("Ses:"), _sesKutusu, Etiket("Hız:"), _hizCubugu, _hizYazisi);
        var butonlar = Satir(_okuBaslat, _okuDuraklat, _okuDurdur, _okuTemizle);

        _okuBaslat.Click += async (_, _) => await OkumayiBaslatAsync();
        _okuDuraklat.Click += (_, _) => _seslendirme.DuraklatVeyaDevamEt();
        _okuDurdur.Click += (_, _) => _seslendirme.Durdur();
        _okuTemizle.Click += (_, _) => { _seslendirme.Durdur(); _okunacakMetin.Clear(); _okunacakMetin.Focus(); };
        _okunacakMetin.TextChanged += (_, _) => ButonlariAyarla();

        return Bolum("Metni Seslendir (Text-to-Speech)", ayarlar, butonlar, _okumaDurumu,
            Etiket("Okunacak metin:"), _okunacakMetin);
    }

    private GroupBox TanimaBolumu()
    {
        _sonucListesi.Columns.Add("Saat", 75);
        _sonucListesi.Columns.Add("Tanınan metin", 400);
        _sonucListesi.Resize += (_, _) => _sonucListesi.Columns[1].Width = Math.Max(100, _sonucListesi.ClientSize.Width - 80);

        var ayarlar = Satir(Etiket("Mikrofon:"), _mikrofonKutusu);
        var butonlar = Satir(_dinleBaslat, _dinleDurdur, _dinleTemizle, _kaydet);

        _dinleBaslat.Click += async (_, _) => await DinlemeyiBaslatAsync();
        _dinleDurdur.Click += async (_, _) => await _tanima.DurdurAsync();
        _dinleTemizle.Click += (_, _) => _tanima.Temizle();
        _kaydet.Click += async (_, _) => await KaydetAsync();

        // Ust: tum metin (G3), alt: zaman damgali liste (O3)
        var ikiAlan = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Margin = Padding.Empty };
        ikiAlan.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        ikiAlan.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        ikiAlan.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        ikiAlan.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        ikiAlan.Controls.Add(Etiket("Tanınan metin:"));
        ikiAlan.Controls.Add(_taninanMetin);
        ikiAlan.Controls.Add(Etiket("Zaman damgalı liste:"));
        ikiAlan.Controls.Add(_sonucListesi);

        return Bolum("Konuşmayı Metne Çevir (Speech-to-Text)", ayarlar, butonlar, _tanimaDurumu, ikiAlan);
    }

    // Basliktan sonra ayarlar, butonlar, durum ve en son tum boslugu dolduran alan(lar).
    private static GroupBox Bolum(string baslik, params Control[] parcalar)
    {
        var kutu = new GroupBox { Text = baslik, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Padding = new Padding(8) };
        var duzen = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = parcalar.Length, Font = new Font("Segoe UI", 9.75f) };
        for (int i = 0; i < parcalar.Length; i++)
        {
            bool sonuncu = i == parcalar.Length - 1;
            duzen.RowStyles.Add(sonuncu ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.AutoSize));
            parcalar[i].Dock = sonuncu ? DockStyle.Fill : parcalar[i].Dock;
            duzen.Controls.Add(parcalar[i]);
        }
        kutu.Controls.Add(duzen);
        return kutu;
    }

    private static FlowLayoutPanel Satir(params Control[] kontroller)
    {
        var satir = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = true, Margin = new Padding(0, 2, 0, 2) };
        satir.Controls.AddRange(kontroller);
        return satir;
    }

    private static Label Etiket(string metin) => new() { Text = metin, AutoSize = true, Margin = new Padding(3, 8, 3, 3) };

    private static Label DurumEtiketi() =>
        new() { AutoSize = true, MaximumSize = new Size(540, 0), Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Margin = new Padding(3, 6, 3, 6) };

    private static Button Buton(string metin, int genislik = 100) => new() { Text = metin, Width = genislik, Height = 32 };

    private double Hiz => _hizCubugu.Value / 10.0;

    // ============================ Acilis ============================

    private void CihazlariYukle()
    {
        try
        {
            IReadOnlyList<SesBilgisi> sesler = _seslendirme.SesleriGetir();
            _sesKutusu.DisplayMember = nameof(SesBilgisi.Ad);
            _sesKutusu.DataSource = sesler.ToList();
            _sesVar = true;
        }
        catch (SeslendirmeHatasi hata)
        {
            // Acilista mesaj kutusu acilmaz: ne yapilacagi durum yazisinda kalici olarak gorunur.
            _gunlukcu.Uyari("Turkce ses yok.");
            _okumaDurumu.Text = "● " + hata.Message;
            _okumaDurumu.ForeColor = Kirmizi;
        }

        IReadOnlyList<string> mikrofonlar = _tanima.Mikrofonlar();
        _mikrofonKutusu.Items.AddRange([.. mikrofonlar]);
        _mikrofonVar = mikrofonlar.Count > 0;
        if (_mikrofonVar)
        {
            _mikrofonKutusu.SelectedIndex = 0;
        }

        if (_sesVar)
        {
            SeslendirmeDurumunuGoster();
        }
        TanimaDurumunuGoster();
    }

    // ============================ Seslendirme ============================

    private async Task OkumayiBaslatAsync()
    {
        if (_sesKutusu.SelectedItem is not SesBilgisi ses)
        {
            return;
        }
        try
        {
            await _seslendirme.BaslatAsync(_okunacakMetin.Text, ses.Kimlik, Hiz);
        }
        catch (SeslendirmeHatasi hata)
        {
            HataGoster(hata.Message);
        }
    }

    private void SeslendirmeDurumunuGoster()
    {
        (string metin, Color renk) = _seslendirme.Durum switch
        {
            SeslendirmeDurumu.Hazirlaniyor => ("● Ses hazırlanıyor...", Turuncu),
            SeslendirmeDurumu.Okunuyor => ("● Okunuyor", Yesil),
            SeslendirmeDurumu.Duraklatildi => ("● Duraklatıldı", Turuncu),
            _ => ("● Hazır", Gri)
        };
        _okumaDurumu.Text = metin;
        _okumaDurumu.ForeColor = renk;
        _okuDuraklat.Text = _seslendirme.Durum == SeslendirmeDurumu.Duraklatildi ? "▶ Devam" : "⏸ Duraklat";
        ButonlariAyarla();
    }

    // ============================ Konusma tanima ============================

    private async Task DinlemeyiBaslatAsync()
    {
        try
        {
            await _tanima.BaslatAsync(_mikrofonKutusu.SelectedIndex);
        }
        catch (Exception hata) when (hata is MikrofonHatasi or TanimaHatasi)
        {
            _gunlukcu.Hata(hata, "Dinleme baslatilamadi.");
            HataGoster(hata.Message);
        }
    }

    private void TanimaDurumunuGoster()
    {
        (string metin, Color renk) = _tanima.Durum switch
        {
            TanimaDurumu.Hazirlaniyor => ("● Konuşma tanıma modeli yükleniyor...", Turuncu),
            TanimaDurumu.Dinleniyor => ("● Dinleniyor — konuşabilirsiniz", Kirmizi),
            TanimaDurumu.Bitiriliyor => ("● Son kısım metne çevriliyor...", Turuncu),
            _ when !_mikrofonVar => ("● Mikrofon bulunamadı", Kirmizi),
            _ => ("● Hazır (mikrofon kapalı)", Gri)
        };
        if (_tanima.CeviriSuruyor && _tanima.Durum == TanimaDurumu.Dinleniyor)
        {
            metin += "  ·  metne çevriliyor...";
        }
        _tanimaDurumu.Text = metin;
        _tanimaDurumu.ForeColor = renk;
        ButonlariAyarla();
    }

    private void SonucuGoster(TanimaSonucu sonuc)
    {
        if (_taninanMetin.TextLength > 0)
        {
            _taninanMetin.AppendText(" ");
        }
        _taninanMetin.AppendText(sonuc.Metin);
        _taninanMetin.ScrollToCaret();

        var satir = new ListViewItem(sonuc.Zaman.ToString("HH:mm:ss"));
        satir.SubItems.Add(sonuc.Metin);
        _sonucListesi.Items.Add(satir);
        satir.EnsureVisible();
        ButonlariAyarla();
    }

    // O2: tanınan metni zaman damgalariyla .txt'ye kaydeder.
    private async Task KaydetAsync()
    {
        using var pencere = new SaveFileDialog
        {
            Filter = "Metin dosyası (*.txt)|*.txt",
            FileName = $"taninan-metin-{DateTime.Now:yyyy-MM-dd-HHmm}.txt",
            Title = "Tanınan metni kaydet"
        };
        if (pencere.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }
        try
        {
            // BOM'lu UTF-8: eski Not Defteri de Turkce karakterleri dogru gostersin.
            await File.WriteAllTextAsync(pencere.FileName, TanimaDokumu.Olustur(_tanima.Sonuclar), new UTF8Encoding(true));
            _gunlukcu.Bilgi($"Tanınan metin kaydedildi ({_tanima.Sonuclar.Count} satir).");
        }
        catch (Exception hata) when (hata is IOException or UnauthorizedAccessException)
        {
            _gunlukcu.Hata(hata, "Txt kaydedilemedi.");
            HataGoster($"Dosya kaydedilemedi: {hata.Message}\nBaşka bir klasör seçip tekrar deneyin.");
        }
    }

    // ============================ Ortak ============================

    private void ButonlariAyarla()
    {
        SeslendirmeDurumu okuma = _seslendirme.Durum;
        _okuBaslat.Enabled = _sesVar && okuma == SeslendirmeDurumu.Hazir && _okunacakMetin.TextLength > 0;
        _okuDuraklat.Enabled = okuma is SeslendirmeDurumu.Okunuyor or SeslendirmeDurumu.Duraklatildi;
        _okuDurdur.Enabled = okuma != SeslendirmeDurumu.Hazir;
        _sesKutusu.Enabled = _hizCubugu.Enabled = _sesVar && okuma == SeslendirmeDurumu.Hazir;

        TanimaDurumu tanima = _tanima.Durum;
        _dinleBaslat.Enabled = _mikrofonVar && tanima == TanimaDurumu.Hazir;
        _dinleDurdur.Enabled = tanima == TanimaDurumu.Dinleniyor;
        _mikrofonKutusu.Enabled = _mikrofonVar && tanima == TanimaDurumu.Hazir;
        _kaydet.Enabled = _tanima.Sonuclar.Count > 0;
        _dinleTemizle.Enabled = _tanima.Sonuclar.Count > 0;
    }

    private void HataGoster(string mesaj) =>
        MessageBox.Show(this, mesaj, "SpeechApp", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    // Kapanirken mikrofon ve ceviri duzgun bitsin (model bellekten atilmadan once son ceviri tamamlanir).
    private async void KapanirkenDurdur(object? gonderen, FormClosingEventArgs e)
    {
        _seslendirme.Durdur();
        if (_kapaniyor || _tanima.Durum == TanimaDurumu.Hazir)
        {
            return;
        }
        e.Cancel = true;
        _kapaniyor = true;
        Enabled = false;
        await _tanima.DurdurAsync();
        Close();
    }
}
