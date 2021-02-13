Param(
    [Parameter(Mandatory = $true, ValueFromPipeline = $True)]
    [IO.FileInfo]$DataFile,
    [string]$FontName
)

$texture = [IO.Path]::GetFileNameWithoutExtension($DataFile.Name).Substring(1)

if (!$FontName) {
    $FontName = [IO.Path]::GetFileNameWithoutExtension($texture).Substring(2, $texture.Length - 6)
    $FontName = [Globalization.CultureInfo]::CurrentCulture.TextInfo.ToTitleCase($FontName)
}

Write-Output @"
Font {
    Name: "$FontName";
    Texture: @"./$($Texture).png";
    Characters: {
"@

Import-CSV $DataFile | Sort -Property {[int]$_.id} | % {
    Write-Output "`t`t$($_.id): [$($_.x) $($_.y) $($_.width) $($_.height) $($_.xoffset) $($_.yoffset) $($_.xadvance)]; # $([char]([int]$_.id))"
}

Write-Output @"
    };
};
"@