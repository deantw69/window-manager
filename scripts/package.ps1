<#
.SYNOPSIS
    建置並打包 WindowManager 發佈版本。

.DESCRIPTION
    產生兩種 self-contained 發佈（皆免安裝 .NET）：
      1. 資料夾版 + ZIP（建議分享給他人，防毒較不易誤判）
      2. 單檔 exe（方便，但自解壓型態較易被防毒盯上）
    輸出到 publish\ 目錄。

.PARAMETER Version
    版本字串；未指定時自 csproj 的 <Version> 讀取。

.PARAMETER Runtime
    目標執行環境，預設 win-x64。

.PARAMETER SkipTests
    跳過單元測試。

.EXAMPLE
    pwsh scripts\package.ps1
    pwsh scripts\package.ps1 -SkipTests
#>
param(
    [string]$Version = "",
    [string]$Runtime = "win-x64",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$proj = Join-Path $root "src\WindowManager\WindowManager.csproj"
$testProj = Join-Path $root "tests\WindowManager.Tests\WindowManager.Tests.csproj"
$outDir = Join-Path $root "publish"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "找不到 dotnet，請先安裝 .NET 8 SDK。"
}

# 版本：未指定則從 csproj 讀取
if (-not $Version) {
    $csproj = Get-Content $proj -Raw
    $m = [regex]::Match($csproj, '<Version>(.*?)</Version>')
    $Version = if ($m.Success) { $m.Groups[1].Value } else { "0.0.0" }
}
Write-Host "==> 版本 $Version / 執行環境 $Runtime" -ForegroundColor Cyan

# 停掉執行中的實例，避免輸出檔被鎖
Get-Process WindowManager -ErrorAction SilentlyContinue | Stop-Process -Force

# 測試
if (-not $SkipTests) {
    Write-Host "==> 執行單元測試" -ForegroundColor Cyan
    dotnet test $testProj -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw "測試失敗，停止打包。" }
}

# 清空輸出目錄
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
New-Item -ItemType Directory -Path $outDir | Out-Null

$common = @(
    "-c", "Release", "-r", $Runtime, "--self-contained", "true",
    "-p:DebugType=none", "-p:DebugSymbols=false", "--nologo"
)

# 1) 資料夾版（建議分享）+ ZIP
Write-Host "==> 發佈資料夾版" -ForegroundColor Cyan
$folder = Join-Path $outDir $Runtime
dotnet publish $proj @common "-p:PublishSingleFile=false" -o $folder
if ($LASTEXITCODE -ne 0) { throw "資料夾版發佈失敗。" }

$zip = Join-Path $outDir "WindowManager-v$Version-$Runtime.zip"
Compress-Archive -Path "$folder\*" -DestinationPath $zip -Force
Write-Host "    ZIP -> $zip" -ForegroundColor Green

# 2) 單檔 exe
Write-Host "==> 發佈單檔 exe" -ForegroundColor Cyan
$single = Join-Path $outDir "single"
dotnet publish $proj @common `
    "-p:PublishSingleFile=true" `
    "-p:IncludeNativeLibrariesForSelfExtract=true" `
    "-p:EnableCompressionInSingleFile=true" -o $single
if ($LASTEXITCODE -ne 0) { throw "單檔發佈失敗。" }
Copy-Item (Join-Path $single "WindowManager.exe") (Join-Path $outDir "WindowManager.exe") -Force

# 摘要
Write-Host "`n==> 完成，輸出於 $outDir" -ForegroundColor Cyan
$zipMB = [math]::Round((Get-Item $zip).Length / 1MB, 1)
$exeMB = [math]::Round((Get-Item (Join-Path $outDir "WindowManager.exe")).Length / 1MB, 1)
Write-Host ("    {0}  ({1} MB)  <- 建議分享" -f (Split-Path $zip -Leaf), $zipMB)
Write-Host ("    WindowManager.exe  ({0} MB)  <- 單檔" -f $exeMB)
