param(
    [string]$FontName, #family or file
        [string]$FontStyle = "Regular",
        [int]$FontSize = 16,
        [string]$FontColor = "White",
    [bool]$Antialias = $true,
    [int]$Padding = 2,
    [int]$MinCodepoint = 32,
    [int]$MaxCodepoint = 255,
    [Switch]$DrawBoundingBoxes,

    [Parameter(Mandatory=$True)]$OutputFile
)
#todo: parameter set

#ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$color = [System.Drawing.Color]::FromName($FontColor)

function Select-Font
{
    $dlg = new-object System.Windows.Forms.FontDialog
    $dlg.Showcolor = $true
    $dlg.FontMustExist = $true
    $dlg.ShowEffects = $true
    $dlg.AllowSimulations = $true
    if ($dlg.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
        throw "Cancelled"
    }
    $color = $dlg.Color
    return $dlg.Font
}

function Load-Font
{
    $pfc = new-object -Type Drawing.Text.PrivateFontCollection
    $pfc.AddFontFile($FontName)
    return [System.Drawing.Font]::new($pfc.Families[0], $FontSize, $FontStyle)
}

$font = if ($FontName)
{
    if (Test-Path $FontName)
    {
        Load-Font 
    }
    else
    {
        [System.Drawing.Font]::new(
            $FontName,
            $FontSize,
            [System.Drawing.FontStyle]$FontStyle
        )
    }
}
else
{
    Select-Font
}

$_bmp = [System.Drawing.Bitmap]::new(1, 1, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb);
$_gfx = [System.Drawing.Graphics]::FromImage($_bmp)
$brush = [System.Drawing.SolidBrush]::new($color)
$format = [System.Drawing.StringFormat]::GenericDefault

function Draw-Character([string]$char)
{
    $fmt = [System.Windows.Forms.TextFormatFlags]::GlyphOverhangPadding
    $size = [System.Windows.Forms.TextRenderer]::MeasureText(
        $char,
        $font,
        [System.Drawing.Size]::new(-1, -1),
        $fmt
    )
    
    $size.Width = [Math]::Ceiling($size.Width - 1 / 4 * $font.Height)
    if ($size.Width -lt 1) {
        return $null
    }

    $bmp = [System.Drawing.Bitmap]::new(
        $size.Width,
        $size.Height, 
        [System.Drawing.Imaging.PixelFormat]::Format16bppRgb555
    )

    $gfx = [System.Drawing.Graphics]::FromImage($bmp)
    
    $gfx.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias

    [System.Windows.Forms.TextRenderer]::DrawText(
        $gfx,
        $char,
        $font,
        [System.Drawing.Point]::new(-1 / 6 * $font.GetHeight(), 0),
        [System.Drawing.Color]::White,
        [System.Drawing.Color]::Black,
        $fmt
    )

    $gfx.Flush()
    $gfx.Dispose()
    
    return $bmp
}

$chars    = New-Object -TypeName System.Collections.Generic.List[int]
$charmaps = New-Object -TypeName System.Collections.Generic.List[System.Drawing.Bitmap]
$xpos     = New-Object -TypeName System.Collections.Generic.List[int]
$ypos     = New-Object -TypeName System.Collections.Generic.List[int]

$totalWidth  = $Padding
$totalHeight = $Padding
$lineWidth   = $Padding
$lineHeight  = $Padding
$lineCount   = 0

$maxLineCount = [int][Math]::Sqrt($MaxCodepoint - $MinCodepoint)

($MinCodepoint..$MaxCodepoint) | % {
    $char = [String]::new($_, 1)
    
    $charmap = Draw-Character $char
    if ($charmap -eq $null) {
        continue
    }

    $chars.Add($_)
    $charmaps.Add($charmap)
    $xpos.Add($lineWidth)
    $ypos.Add($totalHeight)

    $lineWidth += $charmap.Width + $Padding
    $lineHeight = [Math]::Max($lineHeight, $charmap.Height + $Padding)

    if (++$lineCount -eq $maxLineCount) {
        $totalWidth   = [Math]::Max($totalWidth, $lineWidth)
        $totalHeight += $lineHeight
        $lineWidth    = 0
        $lineHeight   = 0
        $lineCount    = 0
    }
}
$totalWidth = [Math]::Max($totalWidth, $lineWidth)
$totalHeight += $lineHeight

$_gfx.Dispose()
$_bmp.Dispose()
$brush.Dispose()
$format.Dispose()

$textureFile = [IO.Path]::ChangeExtension($OutputFile, "png");
@"
# Created from "$($font.Name)" $($font.Style) $($font.Size)em
BitmapFont {
    Texture: @'./$textureFile';
    Tracking: [0 0];
    Characters: {
"@ > $OutputFile

$finalBmp = [System.Drawing.Bitmap]::new(
    $totalWidth, 
    $totalHeight,
    [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
)
$finalGfx = [System.Drawing.Graphics]::FromImage($finalBmp)
$finalGfx.Clear([System.Drawing.Color]::Transparent)
$finalGfx.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy;

$rectPen = [System.Drawing.Pen]::new([System.Drawing.Color]::Red)

for ($i = 0; $i -lt $charmaps.Count; ++$i) {
    $cm = $charmaps[$i]
    #$finalGfx.DrawImage($cm, $xpos[$i], $ypos[$i])

    $finalGfx.DrawImage(
        $cm,
        [System.Drawing.Rectangle]::new($xpos[$i], $ypos[$i], $cm.Width, $cm.Height),
        0, 0, $cm.Width, $cm.Height,
        [System.Drawing.GraphicsUnit]::Pixel,
        $iattr
    )

    if ($DrawBoundingBoxes) {
        $finalGfx.DrawRectangle($rectPen, $xpos[$i], $ypos[$i], $cm.Width, $cm.Height)
    }
    
    "`t`t$($chars[$i]): [$($xpos[$i]) $($ypos[$i]) $($cm.Width) $($cm.Height)];" >> $OutputFile
    $charmaps[$i].Dispose()
}

$finalGfx.Flush()
$rectPen.Dispose()

@"
    };
};
"@ >> $OutputFile

$finalBmp.Save($textureFile, [System.Drawing.Imaging.ImageFormat]::Png)
$finalGfx.Dispose()
$finalBmp.Dispose()