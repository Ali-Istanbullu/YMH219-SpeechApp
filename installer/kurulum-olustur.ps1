# ================================================================================
#  Kurulum dosyasini olusturur (repo kokunden calistirilir):
#    installer\Output\SpeechApp-Kurulum-<surum>.exe
#  Kullanim:
#    powershell -ExecutionPolicy Bypass -File installer\kurulum-olustur.ps1
#  Model kuruluma GOMULMEZ (kurulum sirasinda indirilir), bu yuzden burada gerekmez.
# ================================================================================
param(
    [string]$Iscc = ""
)
$ErrorActionPreference = "Stop"

if (-not $Iscc) {
    $aday = @("$env:USERPROFILE\Desktop\Inno Setup 7\ISCC.exe") +
        (Get-ChildItem "C:\Program Files*\Inno Setup*\ISCC.exe" -ErrorAction SilentlyContinue | ForEach-Object FullName)
    $Iscc = $aday | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
    if (-not $Iscc) { throw "ISCC.exe bulunamadi. -Iscc ile yolunu verin." }
}

Write-Host "== SpeechApp yayinlaniyor" -ForegroundColor Cyan
if (Test-Path publish\SpeechApp) { Remove-Item publish\SpeechApp -Recurse -Force }
dotnet publish src/SpeechApp.WinForms -c Release -r win-x64 --self-contained false -o publish/SpeechApp
if ($LASTEXITCODE -ne 0) { throw "dotnet publish basarisiz" }
# Gelistirme modeli (repo\model) cikti klasorune kopyalanmis olabilir; kuruluma girmez, kurulum indirir.
if (Test-Path publish\SpeechApp\model) { Remove-Item publish\SpeechApp\model -Recurse -Force }

Write-Host "== Kurulum derleniyor" -ForegroundColor Cyan
& $Iscc "installer\SpeechApp.iss"
if ($LASTEXITCODE -ne 0) { throw "Inno Setup derlemesi basarisiz" }

Get-ChildItem installer\Output\*.exe | ForEach-Object { "{0}  {1:N1} MB" -f $_.Name, ($_.Length / 1MB) }
