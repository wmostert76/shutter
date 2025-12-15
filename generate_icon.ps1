Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$ErrorActionPreference = "Stop"

function New-RoundedRectPath([int]$x, [int]$y, [int]$w, [int]$h, [int]$r) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2

    $path.AddArc($x, $y, $d, $d, 180, 90)               | Out-Null
    $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)     | Out-Null
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90) | Out-Null
    $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)      | Out-Null
    $path.CloseFigure() | Out-Null
    return $path
}

$size = 256
$bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)

$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias

# Background: rounded square with subtle gradient
$bgPath = New-RoundedRectPath 10 10 ($size - 20) ($size - 20) 52
$bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point(0, 0)),
    (New-Object System.Drawing.Point($size, $size)),
    ([System.Drawing.Color]::FromArgb(255, 18, 22, 30)),
    ([System.Drawing.Color]::FromArgb(255, 30, 48, 92))
)
$g.FillPath($bgBrush, $bgPath)

$borderPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(90, 255, 255, 255), 6)
$g.DrawPath($borderPen, $bgPath)

# Shutter ring: segmented circle
$cx = [int]($size / 2)
$cy = [int]($size / 2)
$outerR = 98
$innerR = 52

$outerX = $cx - $outerR
$outerY = $cy - $outerR
$outerW = $outerR * 2
$outerH = $outerR * 2
$outerRect = New-Object System.Drawing.Rectangle $outerX, $outerY, $outerW, $outerH
$colors = @(
    [System.Drawing.Color]::FromArgb(255, 82, 180, 255),
    [System.Drawing.Color]::FromArgb(255, 64, 156, 240),
    [System.Drawing.Color]::FromArgb(255, 48, 130, 220),
    [System.Drawing.Color]::FromArgb(255, 64, 156, 240),
    [System.Drawing.Color]::FromArgb(255, 82, 180, 255),
    [System.Drawing.Color]::FromArgb(255, 96, 196, 255)
)

for ($i = 0; $i -lt 6; $i++) {
    $brush = New-Object System.Drawing.SolidBrush($colors[$i])
    $start = 30 + ($i * 60)
    $g.FillPie($brush, $outerRect, $start, 60)
    $brush.Dispose()
}

# Cutout inner circle (gives the segmented ring look)
$innerX = $cx - $innerR
$innerY = $cy - $innerR
$innerW = $innerR * 2
$innerH = $innerR * 2
$innerRect = New-Object System.Drawing.Rectangle $innerX, $innerY, $innerW, $innerH
$cutBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 15, 18, 26))
$g.FillEllipse($cutBrush, $innerRect)

$innerPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(120, 255, 255, 255), 5)
$g.DrawEllipse($innerPen, $innerRect)

# Power symbol overlay (clear in tray size)
$pR = 54
$pX = $cx - $pR
$pY = $cy - $pR
$pW = $pR * 2
$pH = $pR * 2
$pRect = New-Object System.Drawing.Rectangle $pX, $pY, $pW, $pH
$shadowPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(90, 0, 0, 0), 18)
$powerPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(245, 255, 255, 255), 18)
$powerPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$powerPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$shadowPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$shadowPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

# Shadow (slight offset)
$shadowRect = New-Object System.Drawing.Rectangle ($pRect.X + 2), ($pRect.Y + 3), $pRect.Width, $pRect.Height
$g.DrawArc($shadowPen, $shadowRect, 315, 270)
$g.DrawLine($shadowPen, $cx + 2, ($cy - $pR - 10) + 3, $cx + 2, $cy + 10 + 3)

$g.DrawArc($powerPen, $pRect, 315, 270)
$g.DrawLine($powerPen, $cx, ($cy - $pR - 10), $cx, $cy + 10)

# Convert to icon and save
$hIcon = $bmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)
$fs = New-Object System.IO.FileStream("app.ico", [System.IO.FileMode]::Create)
$icon.Save($fs)
$fs.Close()

# Cleanup
$icon.Dispose()
$powerPen.Dispose()
$shadowPen.Dispose()
$innerPen.Dispose()
$cutBrush.Dispose()
$borderPen.Dispose()
$bgBrush.Dispose()
$bgPath.Dispose()
$g.Dispose()
$bmp.Dispose()

Write-Host "Generated app.ico"
