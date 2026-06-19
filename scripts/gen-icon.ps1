# 產生應用程式圖示 app.ico(藍底白視窗,PNG-embedded 多尺寸)
# 用法:pwsh -File scripts/gen-icon.ps1
Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$outDir = Join-Path $root 'src/WindowManager/Resources'
$outIco = Join-Path $outDir 'app.ico'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

function New-RoundedPath([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

function New-IconBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)

    # 背景:藍色漸層圓角方塊
    $pad = [float]($size * 0.05)
    $bw = [float]($size - 2 * $pad)
    $bgPath = New-RoundedPath $pad $pad $bw $bw ([float]($size * 0.20))
    $rect = New-Object System.Drawing.RectangleF($pad, $pad, $bw, $bw)
    $top = [System.Drawing.Color]::FromArgb(255, 61, 140, 224)
    $bot = [System.Drawing.Color]::FromArgb(255, 27, 82, 153)
    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $top, $bot, 90)
    $g.FillPath($bgBrush, $bgPath)

    # 視窗:白色圓角矩形
    $ww = [float]($size * 0.54)
    $wh = [float]($size * 0.44)
    $wx = [float](($size - $ww) / 2)
    $wy = [float](($size - $wh) / 2)
    $rad = [Math]::Max(1.0, [float]($size * 0.05))
    $winPath = New-RoundedPath $wx $wy $ww $wh $rad
    $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $g.FillPath($white, $winPath)

    # 標題列:淺藍
    $titleH = [float]($wh * 0.30)
    $g.SetClip($winPath)
    $titleRect = New-Object System.Drawing.RectangleF($wx, $wy, $ww, $titleH)
    $titleBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 210, 228, 247))
    $g.FillRectangle($titleBrush, $titleRect)
    $g.ResetClip()

    # 標題列小圓點(尺寸夠大才畫)
    if ($size -ge 32) {
        $dotR = [float]($titleH * 0.22)
        $dotY = [float]($wy + $titleH / 2 - $dotR)
        $dotBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 27, 82, 153))
        for ($i = 0; $i -lt 3; $i++) {
            $dotX = [float]($wx + $ww * 0.10 + $i * $dotR * 3.2)
            $g.FillEllipse($dotBrush, $dotX, $dotY, $dotR * 2, $dotR * 2)
        }
        $dotBrush.Dispose()
    }

    $g.Dispose()
    $bgBrush.Dispose(); $white.Dispose(); $titleBrush.Dispose()
    $bgPath.Dispose(); $winPath.Dispose()
    return $bmp
}

# 各尺寸轉 PNG bytes
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$pngs = @()
foreach ($s in $sizes) {
    $bmp = New-IconBitmap $s
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += , $ms.ToArray()
    $ms.Dispose(); $bmp.Dispose()
}

# 組成 ICO(全部以 PNG 內嵌)
$fs = New-Object System.IO.FileStream($outIco, [System.IO.FileMode]::Create)
$bw = New-Object System.IO.BinaryWriter($fs)
$count = $pngs.Count
$bw.Write([uint16]0)      # reserved
$bw.Write([uint16]1)      # type = icon
$bw.Write([uint16]$count) # count

$offset = 6 + 16 * $count
for ($i = 0; $i -lt $count; $i++) {
    $s = $sizes[$i]
    $len = $pngs[$i].Length
    $dim = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$dim)   # width
    $bw.Write([byte]$dim)   # height
    $bw.Write([byte]0)      # palette
    $bw.Write([byte]0)      # reserved
    $bw.Write([uint16]1)    # planes
    $bw.Write([uint16]32)   # bpp
    $bw.Write([uint32]$len) # bytes
    $bw.Write([uint32]$offset)
    $offset += $len
}
foreach ($png in $pngs) { $bw.Write($png) }
$bw.Flush(); $bw.Close(); $fs.Close()

Write-Host "已產生:$outIco ($((Get-Item $outIco).Length) bytes,$count 個尺寸)"
