; ================================================================================
;  SpeechApp kurulum betigi (Inno Setup)
;
;  En kolayi: repo kokunden  powershell -ExecutionPolicy Bypass -File installer\kurulum-olustur.ps1
;  Elle:
;    dotnet publish src/SpeechApp.WinForms -c Release -r win-x64 --self-contained false -o publish/SpeechApp
;    ISCC.exe installer\SpeechApp.iss
;  Cikti: installer\Output\SpeechApp-Kurulum-1.0.0.exe
;
;  Gereksinimler (yoksa indirilip sessizce kurulur; olmazsa kurulum hata verip durur):
;    - .NET 10 Desktop Runtime (x64)
;    - Microsoft Visual C++ 2015-2022 calisma zamani (x64): whisper.cpp DLL'leri VCRUNTIME140/MSVCP140/VCOMP140 ister
;    - Whisper konusma tanima modeli ggml-small-q8_0.bin (252 MiB, Hugging Face): indirilir, SHA-256 ile dogrulanir,
;      {app}\model'e kopyalanir. Kurulum dosyasi bu yuzden kucuk kalir.
;  Kaldirinca (Ali karari): program klasoru ve model silinir; %LocalAppData%\SpeechApp (loglar) KALIR.
; ================================================================================

#define UygulamaAdi "SpeechApp"
#define GorunenAd "SpeechApp"
#define UygulamaSurumu "1.0.0"
#define Yayinci "Ali İstanbullu"
#define ExeAdi "SpeechApp.exe"
#define YayinKlasoru "..\publish\SpeechApp"

#define RuntimeUrl "https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe"
#define RuntimeDosyasi "windowsdesktop-runtime-win-x64.exe"
#define VcUrl "https://aka.ms/vs/17/release/vc_redist.x64.exe"
#define VcDosyasi "vc_redist.x64.exe"

; Model: Hugging Face ggerganov/whisper.cpp deposu. SHA-256, Hugging Face'in kendi kaydindaki (Git LFS oid) deger.
#define ModelUrl "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small-q8_0.bin"
#define ModelDosyasi "ggml-small-q8_0.bin"
#define ModelSha256 "49c8fb02b65e6049d5fa6c04f81f53b867b5ec9540406812c643f177317f779f"

[Setup]
AppId={{4C8E2F57-9A1B-4D3E-B6C2-7F0A5E8D1C39}
AppName={#GorunenAd}
AppVersion={#UygulamaSurumu}
AppPublisher={#Yayinci}
DefaultDirName={autopf}\{#UygulamaAdi}
DefaultGroupName={#GorunenAd}
PrivilegesRequired=admin
; x64 Windows 10/11. Arm64 Windows reddedilir: orada x64 .NET "dotnet\x64" klasorune kurulur, test edilmedi.
ArchitecturesAllowed=x64compatible and not arm64
; .NET 10, Windows 7/8.1 desteklemez: kurulum orada hic baslamaz.
MinVersion=10.0
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=Output
OutputBaseFilename={#UygulamaAdi}-Kurulum-{#UygulamaSurumu}
SetupIconFile=..\assets\speechapp.ico
UninstallDisplayIcon={app}\{#ExeAdi}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "masaustu"; Description: "Masaüstüne kısayol oluştur"; GroupDescription: "Kısayollar:"
Name: "baslatmenusu"; Description: "Başlat menüsüne kısayol oluştur"; GroupDescription: "Kısayollar:"

[Files]
; Sadece x64 Windows DLL'leri: x86, Arm64 ve Linux kopyalari kuruluma girmez (Whisper.net paketi hepsini getirir).
Source: "{#YayinKlasoru}\*"; DestDir: "{app}"; Excludes: "*.pdb,\runtimes\win-x86\*,\runtimes\win-arm64\*,\runtimes\noavx\win-x86\*,\runtimes\noavx\linux-x64\*"; Flags: ignoreversion recursesubdirs createallsubdirs
; Model: kurulum sirasinda {tmp}'ye indirilir (asagidaki kod), buradan kopyalanir. Zaten dogru model varsa indirilmez.
Source: "{tmp}\{#ModelDosyasi}"; DestDir: "{app}\model"; Flags: external ignoreversion; Check: ModelIndirildi

[Icons]
Name: "{autodesktop}\{#GorunenAd}"; Filename: "{app}\{#ExeAdi}"; Tasks: masaustu
Name: "{group}\{#GorunenAd}"; Filename: "{app}\{#ExeAdi}"; Tasks: baslatmenusu
Name: "{group}\{#GorunenAd} Kaldır"; Filename: "{uninstallexe}"; Tasks: baslatmenusu

[UninstallDelete]
; Program klasoru (model dahil) TAMAMEN silinir. %LocalAppData%\SpeechApp (loglar) bilerek SILINMEZ.
Type: filesandordirs; Name: "{app}"

[Run]
Filename: "{app}\{#ExeAdi}"; Description: "SpeechApp'i başlat"; Flags: nowait postinstall skipifsilent

[Code]
var
  IndirmeSayfasi: TDownloadWizardPage;   // indirilen dosyalar {tmp}'ye gelir, kurulum bitince silinir
  ModelIndi: Boolean;

procedure InitializeWizard;
begin
  IndirmeSayfasi := CreateDownloadPage('Gerekli dosyalar indiriliyor',
    'SpeechApp için gerekli bileşenler ve konuşma tanıma modeli indiriliyor, lütfen bekleyin.', nil);
end;

function ModelIndirildi: Boolean;
begin
  Result := ModelIndi;
end;

// .NET 10 Desktop Runtime (x64) kurulu mu? Microsoft'un kurdugu klasorde 10.x surumu aranir.
function RuntimeKuruluMu: Boolean;
var
  Bulunan: TFindRec;
begin
  Result := FindFirst(ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App\10.*'), Bulunan);
  if Result then
    FindClose(Bulunan);
end;

// Visual C++ 2015-2022 (x64) calisma zamani kurulu mu? Microsoft'un yazdigi kayit defteri degeri.
function VcKuruluMu: Boolean;
var
  Kurulu: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Installed', Kurulu)
            and (Kurulu = 1);
end;

// Guncellemede model zaten varsa ve SHA-256'si dogruysa tekrar indirilmez.
function ModelKuruluMu: Boolean;
var
  Yol: String;
begin
  Yol := ExpandConstant('{app}\model\{#ModelDosyasi}');
  Result := FileExists(Yol) and (CompareText(GetSHA256OfFile(Yol), '{#ModelSha256}') = 0);
end;

// Indirilen kurulum programini sessizce calistirir. Basarisizsa False ve hata mesaji.
// 0 = basarili, 3010/1641 = basarili ama yeniden baslatma gerekli, 1638 = daha yeni surumu zaten kurulu.
function SessizKur(Dosya, Ad: String; var Hata: String): Boolean;
var
  Sayfa: TOutputMarqueeProgressWizardPage;
  CikisKodu: Integer;
begin
  Result := False;
  Sayfa := CreateOutputMarqueeProgressPage(Ad + ' kuruluyor', 'Bu birkaç dakika sürebilir, lütfen bekleyin.');
  Sayfa.Show;
  try
    Sayfa.Animate;
    if not Exec(ExpandConstant('{tmp}\') + Dosya, '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, CikisKodu) then
    begin
      Hata := Ad + ' kurulum programı çalıştırılamadı: ' + SysErrorMessage(CikisKodu);
      Exit;
    end;
    if (CikisKodu <> 0) and (CikisKodu <> 3010) and (CikisKodu <> 1641) and (CikisKodu <> 1638) then
    begin
      Hata := Ad + ' kurulumu başarısız oldu (çıkış kodu ' + IntToStr(CikisKodu) + ').';
      Exit;
    end;
    Result := True;
  finally
    Sayfa.Hide;
  end;
end;

// "Kur" dugmesine basilinca: eksik bilesenler ve model indirilir, bilesenler kurulur. Hata olursa kurulum ilerlemez.
function NextButtonClick(CurPageID: Integer): Boolean;
var
  Hata: String;
  RuntimeGerekli, VcGerekli: Boolean;
begin
  Result := True;
  if CurPageID <> wpReady then
    Exit;
  RuntimeGerekli := not RuntimeKuruluMu;
  VcGerekli := not VcKuruluMu;
  ModelIndi := not ModelKuruluMu;
  if not (RuntimeGerekli or VcGerekli or ModelIndi) then
    Exit;

  IndirmeSayfasi.Clear;
  if RuntimeGerekli then
    IndirmeSayfasi.Add('{#RuntimeUrl}', '{#RuntimeDosyasi}', '');
  if VcGerekli then
    IndirmeSayfasi.Add('{#VcUrl}', '{#VcDosyasi}', '');
  // Ucuncu parametre: inen dosyanin SHA-256'si tutmazsa Inno Setup indirmeyi hata sayar (bozuk/yarim model kurulmaz).
  if ModelIndi then
    IndirmeSayfasi.Add('{#ModelUrl}', '{#ModelDosyasi}', '{#ModelSha256}');
  IndirmeSayfasi.Show;
  try
    try
      IndirmeSayfasi.Download;
    except
      if not IndirmeSayfasi.AbortedByUser then
        SuppressibleMsgBox('Gerekli dosyalar indirilemedi. İnternet bağlantınızı kontrol edip tekrar deneyin.' + #13#10#13#10 +
          AddPeriod(GetExceptionMessage), mbCriticalError, MB_OK, IDOK);
      Result := False;
      Exit;
    end;
  finally
    IndirmeSayfasi.Hide;
  end;

  if RuntimeGerekli then
  begin
    if not SessizKur('{#RuntimeDosyasi}', '.NET 10 Desktop Runtime', Hata) or not RuntimeKuruluMu then
    begin
      if Hata = '' then Hata := '.NET 10 Desktop Runtime kuruldu görünüyor ama bulunamadı.';
      SuppressibleMsgBox(Hata + #13#10#13#10 + 'SpeechApp kurulamadı.', mbCriticalError, MB_OK, IDOK);
      Result := False;
      Exit;
    end;
  end;

  if VcGerekli then
  begin
    if not SessizKur('{#VcDosyasi}', 'Microsoft Visual C++ çalışma zamanı', Hata) or not VcKuruluMu then
    begin
      if Hata = '' then Hata := 'Microsoft Visual C++ çalışma zamanı kuruldu görünüyor ama bulunamadı.';
      SuppressibleMsgBox(Hata + #13#10#13#10 + 'SpeechApp kurulamadı.', mbCriticalError, MB_OK, IDOK);
      Result := False;
    end;
  end;
end;
