<#
.SYNOPSIS
    打包並在 GitHub 發布一版 Release（手動、可控）。

.DESCRIPTION
    流程：打包（呼叫 package.ps1）→ 建立並推送 tag → 建立 GitHub Release →
    上傳 ZIP 與單檔 exe。認證沿用本機 Git Credential Manager 既有的 GitHub token。

.PARAMETER Version
    版本字串；未指定時自 csproj 的 <Version> 讀取。Release 與 tag 名為 v<Version>。

.PARAMETER Repo
    GitHub repo（owner/name），預設 deantw69/window-manager。

.PARAMETER Runtime
    目標執行環境，預設 win-x64。

.PARAMETER SkipPackage
    跳過重新打包，直接用 publish\ 既有產物。

.PARAMETER Draft
    建立為草稿 Release（不公開，需手動發佈）。

.PARAMETER PreRelease
    標記為 pre-release。

.PARAMETER DryRun
    只顯示將執行的動作，不實際建立 tag / Release / 上傳。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\release.ps1
    powershell -ExecutionPolicy Bypass -File scripts\release.ps1 -DryRun
    powershell -ExecutionPolicy Bypass -File scripts\release.ps1 -Version 1.1.0
#>
param(
    [string]$Version = "",
    [string]$Repo = "deantw69/window-manager",
    [string]$Runtime = "win-x64",
    [switch]$SkipPackage,
    [switch]$Draft,
    [switch]$PreRelease,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$root = Split-Path $PSScriptRoot -Parent
$proj = Join-Path $root "src\WindowManager\WindowManager.csproj"
$outDir = Join-Path $root "publish"
$changelog = Join-Path $root "CHANGELOG.md"

# --- 版本 ---
if (-not $Version) {
    $m = [regex]::Match((Get-Content $proj -Raw), '<Version>(.*?)</Version>')
    if (-not $m.Success) { throw "無法從 csproj 取得 <Version>，請用 -Version 指定。" }
    $Version = $m.Groups[1].Value
}
$tag = "v$Version"
Write-Host "==> 發布 $tag  ($Repo)" -ForegroundColor Cyan

# --- 取得 GitHub token（沿用本機認證） ---
# 注意：PowerShell 直接 pipe 到 git credential 會夾帶 BOM 導致失敗，
# 改用乾淨的 ASCII 暫存檔經 cmd 的 < 重導輸入。
$credTmp = [IO.Path]::GetTempFileName()
[IO.File]::WriteAllText($credTmp, "protocol=https`nhost=github.com`n`n", (New-Object System.Text.ASCIIEncoding))
$cred = cmd /c "git credential fill < `"$credTmp`"" 2>$null
Remove-Item $credTmp -Force -ErrorAction SilentlyContinue
$token = (@($cred | Where-Object { $_ -like 'password=*' })[0]) -replace '^password=', ''
if (-not $token) { throw "找不到本機 GitHub 認證；請先以 git 推送過一次，或設定 Git Credential Manager。" }
$headers = @{ Authorization = "token $token"; "User-Agent" = "window-manager-release"; Accept = "application/vnd.github+json" }

# --- 打包 ---
if (-not $SkipPackage) {
    Write-Host "==> 打包" -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot "package.ps1") -Version $Version -Runtime $Runtime
    if ($LASTEXITCODE -ne 0) { throw "打包失敗。" }
}

$zip = Join-Path $outDir "WindowManager-v$Version-$Runtime.zip"
$exe = Join-Path $outDir "WindowManager.exe"
foreach ($f in @($zip, $exe)) {
    if (-not (Test-Path $f)) { throw "找不到資產 $f；請先打包（移除 -SkipPackage）。" }
}

# --- 釋出說明：優先取 CHANGELOG 對應版本段落 ---
$notes = ""
if (Test-Path $changelog) {
    $text = Get-Content $changelog -Raw
    $mm = [regex]::Match($text, "(?ms)^##\s*\[$([regex]::Escape($Version))\].*?(?=^##\s*\[|\z)")
    if ($mm.Success) { $notes = $mm.Value.Trim() }
}
if (-not $notes) {
    $notes = "視窗管理員 $tag。詳見 CHANGELOG。"
}
$notes += "`n`n---`n下載 **WindowManager-v$Version-$Runtime.zip**（建議，防毒較不易誤判）或單檔 **WindowManager.exe**。`n若被 SmartScreen/防毒擋下：右鍵內容勾「解除封鎖」，或 SmartScreen「其他資訊→仍要執行」。"

# --- 檢查 Release 是否已存在 ---
$existing = $null
try {
    $existing = Invoke-RestMethod -Method Get -Headers $headers -Uri "https://api.github.com/repos/$Repo/releases/tags/$tag"
} catch { }
if ($existing) { throw "Release $tag 已存在（id=$($existing.id)）。請改版本號，或先到 GitHub 刪除該 Release 再重跑。" }

if ($DryRun) {
    Write-Host "`n[DryRun] 將執行下列動作：" -ForegroundColor Yellow
    Write-Host "  - 建立並推送 tag $tag（若不存在）"
    Write-Host "  - 建立 Release $tag (draft=$($Draft.IsPresent), prerelease=$($PreRelease.IsPresent))"
    Write-Host "  - 上傳：$(Split-Path $zip -Leaf), WindowManager.exe"
    Write-Host "`n釋出說明預覽：`n$notes" -ForegroundColor DarkGray
    return
}

# --- 建立並推送 tag（若本機沒有） ---
$hasTag = (git tag --list $tag)
if (-not $hasTag) {
    git tag -a $tag -m "視窗管理員 $tag"
    Write-Host "    已建立 tag $tag"
}
# 注意：勿用 2>&1，否則 PowerShell 5.1 會把 git 的 stderr 進度訊息包成
# NativeCommandError，在 ErrorActionPreference=Stop 下誤判為失敗而中止。
git push origin $tag
if ($LASTEXITCODE -ne 0) { throw "推送 tag $tag 失敗。" }
Write-Host "    已推送 tag $tag"

# --- 建立 Release ---
Write-Host "==> 建立 Release" -ForegroundColor Cyan
$body = @{
    tag_name   = $tag
    name       = $tag
    body       = $notes
    draft      = [bool]$Draft
    prerelease = [bool]$PreRelease
} | ConvertTo-Json
$rel = Invoke-RestMethod -Method Post -Headers $headers -ContentType "application/json; charset=utf-8" `
    -Uri "https://api.github.com/repos/$Repo/releases" -Body ([Text.Encoding]::UTF8.GetBytes($body))
Write-Host "    $($rel.html_url)" -ForegroundColor Green

# --- 上傳資產 ---
# 注意：upload_url 與 headers 以參數明確傳入函式，不要依賴函式自動讀取 script-scope 變數。
$uploadBase = $rel.upload_url -replace '\{.*\}', ''
if (-not $uploadBase) { throw "無法從 Release 回應取得 upload_url。" }

function Send-Asset($base, $hdr, $path, $contentType) {
    $name = Split-Path $path -Leaf
    Write-Host "==> 上傳 $name" -ForegroundColor Cyan
    Invoke-RestMethod -Method Post -Headers $hdr -ContentType $contentType `
        -Uri ("{0}?name={1}" -f $base, $name) -InFile $path | Out-Null
    Write-Host "    OK"
}
Send-Asset $uploadBase $headers $zip "application/zip"
Send-Asset $uploadBase $headers $exe "application/octet-stream"

Write-Host "`n==> 完成：$($rel.html_url)" -ForegroundColor Green
