<#
.SYNOPSIS
    Generates all high-DPI Windows App SDK asset variants and multi-resolution app.ico
    for the OmniForge Utility Suite.

.DESCRIPTION
    Uses System.Drawing and GDI+ to render crisp, anti-aliased geometric anvil/prism
    logos in multiple scales and formats required for Package.appxmanifest and Win32 icon loading.
#>

[CmdletBinding()]
param(
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = if ($PSScriptRoot) { $PSScriptRoot } else { ".\src\OmniForge.App\Assets" }
}
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)
Add-Type -AssemblyName System.Drawing

function Render-OmniForgeGraphic {
    param(
        [int]$Width,
        [int]$Height,
        [bool]$IsSplashScreen = $false
    )

    $bitmap = [System.Drawing.Bitmap]::new($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

    $g.Clear([System.Drawing.Color]::Transparent)

    if ($IsSplashScreen) {
        $iconDimension = [Math]::Min($Width, $Height) * 0.65
        $offsetX = ($Width - $iconDimension) / 2
        $offsetY = ($Height - $iconDimension) / 2
        $scale = $iconDimension / 512.0
    } else {
        $scale = [Math]::Min($Width, $Height) / 512.0
        $offsetX = ($Width - (512.0 * $scale)) / 2
        $offsetY = ($Height - (512.0 * $scale)) / 2
    }

    $g.TranslateTransform($offsetX, $offsetY)
    $g.ScaleTransform($scale, $scale)

    # Gradients
    $cyan = [System.Drawing.Color]::FromArgb(255, 0, 240, 255)
    $blue = [System.Drawing.Color]::FromArgb(255, 0, 120, 212)
    $purple = [System.Drawing.Color]::FromArgb(255, 121, 40, 202)
    $deepPurple = [System.Drawing.Color]::FromArgb(255, 45, 17, 82)

    $pt0 = [System.Drawing.PointF]::new(0, 0)
    $pt512 = [System.Drawing.PointF]::new(512, 512)
    $pt0_512 = [System.Drawing.PointF]::new(0, 512)
    $pt512_0 = [System.Drawing.PointF]::new(512, 0)

    $primaryBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new($pt0, $pt512, $cyan, $purple)
    $secondaryBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new($pt0_512, $pt512_0, $purple, $cyan)
    $baseBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new($pt0, $pt512, $blue, $deepPurple)

    # 1. Base Pedestal
    $basePath = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $basePoints = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(96, 444),
        [System.Drawing.PointF]::new(416, 444),
        [System.Drawing.PointF]::new(392, 408),
        [System.Drawing.PointF]::new(120, 408)
    )
    $basePath.AddPolygon($basePoints)
    $g.FillPath($baseBrush, $basePath)
    $basePen = [System.Drawing.Pen]::new($cyan, 2)
    $g.DrawPath($basePen, $basePath)

    # 2. Middle Risers
    $riserPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $riserPoints = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(124, 408),
        [System.Drawing.PointF]::new(388, 408),
        [System.Drawing.PointF]::new(364, 360),
        [System.Drawing.PointF]::new(148, 360)
    )
    $riserPath.AddPolygon($riserPoints)
    $g.FillPath($baseBrush, $riserPath)

    # 3. Anvil Waist
    $waistPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $waistPoints = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(176, 360),
        [System.Drawing.PointF]::new(336, 360),
        [System.Drawing.PointF]::new(308, 290),
        [System.Drawing.PointF]::new(204, 290)
    )
    $waistPath.AddPolygon($waistPoints)
    $g.FillPath($secondaryBrush, $waistPath)

    # 4. Left Horn
    $hornPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $hornPoints = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(64, 196),
        [System.Drawing.PointF]::new(204, 196),
        [System.Drawing.PointF]::new(216, 280),
        [System.Drawing.PointF]::new(168, 280),
        [System.Drawing.PointF]::new(140, 248)
    )
    $hornPath.AddPolygon($hornPoints)
    $g.FillPath($primaryBrush, $hornPath)

    # 5. Top Plate
    $topPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $topPoints = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(176, 132),
        [System.Drawing.PointF]::new(400, 132),
        [System.Drawing.PointF]::new(448, 196),
        [System.Drawing.PointF]::new(204, 196)
    )
    $topPath.AddPolygon($topPoints)
    $g.FillPath($primaryBrush, $topPath)

    # 6. Right Heel
    $heelPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $heelPoints = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(400, 132),
        [System.Drawing.PointF]::new(448, 196),
        [System.Drawing.PointF]::new(448, 272),
        [System.Drawing.PointF]::new(384, 288),
        [System.Drawing.PointF]::new(320, 288),
        [System.Drawing.PointF]::new(320, 240),
        [System.Drawing.PointF]::new(376, 224),
        [System.Drawing.PointF]::new(400, 196)
    )
    $heelPath.AddPolygon($heelPoints)
    $g.FillPath($secondaryBrush, $heelPath)

    # 7. Front Body Cut
    $bodyPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $bodyPoints = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(204, 196),
        [System.Drawing.PointF]::new(384, 196),
        [System.Drawing.PointF]::new(352, 284),
        [System.Drawing.PointF]::new(216, 280)
    )
    $bodyPath.AddPolygon($bodyPoints)
    $g.FillPath($primaryBrush, $bodyPath)

    # 8. Central Prismatic Core Shard
    $shardPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $shardPoints = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(256, 152),
        [System.Drawing.PointF]::new(300, 216),
        [System.Drawing.PointF]::new(256, 280),
        [System.Drawing.PointF]::new(212, 216)
    )
    $shardPath.AddPolygon($shardPoints)
    $shardBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(240, 255, 255, 255))
    $g.FillPath($shardBrush, $shardPath)

    # Highlight strokes
    $glowPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(200, 0, 255, 255), 3)
    $g.DrawLine($glowPen, 80, 206, 192, 206)
    $g.DrawLine($glowPen, 192, 206, 220, 270)
    $g.DrawLine($glowPen, 124, 408, 388, 408)

    # Clean up GDI+ resources
    $g.Dispose()
    $primaryBrush.Dispose()
    $secondaryBrush.Dispose()
    $baseBrush.Dispose()
    $shardBrush.Dispose()
    $basePen.Dispose()
    $glowPen.Dispose()
    $basePath.Dispose()
    $riserPath.Dispose()
    $waistPath.Dispose()
    $hornPath.Dispose()
    $topPath.Dispose()
    $heelPath.Dispose()
    $bodyPath.Dispose()
    $shardPath.Dispose()

    return $bitmap
}

function Build-IcoFile {
    param(
        [System.Drawing.Bitmap[]]$Bitmaps,
        [string]$DestinationPath
    )

    $pngStreams = [System.Collections.Generic.List[byte[]]]::new()
    foreach ($bmp in $Bitmaps) {
        $ms = [System.IO.MemoryStream]::new()
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngStreams.Add($ms.ToArray())
        $ms.Dispose()
    }

    $fs = [System.IO.File]::Create($DestinationPath)
    $bw = [System.IO.BinaryWriter]::new($fs)

    try {
        # ICONDIR header: idReserved (2), idType (2), idCount (2)
        $bw.Write([uint16]0)
        $bw.Write([uint16]1) # Icon
        $bw.Write([uint16]$Bitmaps.Length)

        $offset = 6 + ($Bitmaps.Length * 16)

        for ($i = 0; $i -lt $Bitmaps.Length; $i++) {
            $bmp = $Bitmaps[$i]
            $bytes = $pngStreams[$i]

            $w = if ($bmp.Width -ge 256) { [byte]0 } else { [byte]$bmp.Width }
            $h = if ($bmp.Height -ge 256) { [byte]0 } else { [byte]$bmp.Height }

            $bw.Write($w)                         # bWidth
            $bw.Write($h)                         # bHeight
            $bw.Write([byte]0)                    # bColorCount
            $bw.Write([byte]0)                    # bReserved
            $bw.Write([uint16]1)                  # wPlanes
            $bw.Write([uint16]32)                 # wBitCount
            $bw.Write([uint32]$bytes.Length)      # dwBytesInRes
            $bw.Write([uint32]$offset)            # dwImageOffset

            $offset += $bytes.Length
        }

        # Write image bytes
        foreach ($bytes in $pngStreams) {
            $bw.Write($bytes)
        }
    }
    finally {
        $bw.Dispose()
        $fs.Dispose()
    }
}

Write-Host "OmniForge Asset Engine: Initializing high-DPI asset generation..." -ForegroundColor Cyan

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$variants = @(
    @{ Name = "Square44x44Logo.scale-100.png"; Width = 44; Height = 44 },
    @{ Name = "Square44x44Logo.scale-125.png"; Width = 55; Height = 55 },
    @{ Name = "Square44x44Logo.scale-150.png"; Width = 66; Height = 66 },
    @{ Name = "Square44x44Logo.scale-200.png"; Width = 88; Height = 88 },
    @{ Name = "Square44x44Logo.scale-400.png"; Width = 176; Height = 176 },
    @{ Name = "Square150x150Logo.scale-100.png"; Width = 150; Height = 150 },
    @{ Name = "Square150x150Logo.scale-125.png"; Width = 188; Height = 188 },
    @{ Name = "Square150x150Logo.scale-150.png"; Width = 225; Height = 225 },
    @{ Name = "Square150x150Logo.scale-200.png"; Width = 300; Height = 300 },
    @{ Name = "Square150x150Logo.scale-400.png"; Width = 600; Height = 600 },
    @{ Name = "SplashScreen.scale-100.png"; Width = 620; Height = 300; Splash = $true },
    @{ Name = "SplashScreen.scale-150.png"; Width = 930; Height = 450; Splash = $true },
    @{ Name = "SplashScreen.scale-200.png"; Width = 1240; Height = 600; Splash = $true },
    @{ Name = "StoreLogo.scale-100.png"; Width = 50; Height = 50 },
    @{ Name = "StoreLogo.scale-200.png"; Width = 100; Height = 100 },
    @{ Name = "StoreLogo.scale-400.png"; Width = 200; Height = 200 },
    @{ Name = "Square44x44Logo.targetsize-24_altform-unplated.png"; Width = 24; Height = 24 },
    @{ Name = "Square44x44Logo.targetsize-48_altform-lightunplated.png"; Width = 48; Height = 48 }
)

foreach ($v in $variants) {
    $targetPath = Join-Path $OutputDir $v.Name
    $isSplash = if ($v.ContainsKey("Splash")) { $v.Splash } else { $false }
    $bmp = Render-OmniForgeGraphic -Width $v.Width -Height $v.Height -IsSplashScreen $isSplash
    $bmp.Save($targetPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  -> Generated: $($v.Name) ($($v.Width)x$($v.Height))" -ForegroundColor Green
}

# Overwrite StoreLogo.png with StoreLogo.scale-100.png for unscaled fallback
$storeLogoDefault = Join-Path $OutputDir "StoreLogo.png"
Copy-Item -Path (Join-Path $OutputDir "StoreLogo.scale-100.png") -Destination $storeLogoDefault -Force

# Generate multi-layer app.ico
$icoSizes = @(16, 24, 32, 48, 64, 128, 256)
$icoBitmaps = [System.Collections.Generic.List[System.Drawing.Bitmap]]::new()
foreach ($sz in $icoSizes) {
    $icoBitmaps.Add((Render-OmniForgeGraphic -Width $sz -Height $sz -IsSplashScreen $false))
}

$icoPath = Join-Path $OutputDir "app.ico"
Build-IcoFile -Bitmaps $icoBitmaps.ToArray() -DestinationPath $icoPath
foreach ($b in $icoBitmaps) { $b.Dispose() }
Write-Host "  -> Generated multi-layer icon: app.ico (16, 24, 32, 48, 64, 128, 256 px)" -ForegroundColor Magenta

Write-Host "OmniForge Asset Generation: SUCCESS!" -ForegroundColor Cyan
