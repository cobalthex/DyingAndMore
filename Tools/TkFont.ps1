Param(
    [Parameter(Mandatory = $true, ValueFromPipeline = $True)]
    [string]$InputFile,
    [string]$FontName
)

$texture = [IO.Path]::GetFileNameWithoutExtension($InputFile).Substring(1)

if (!$FontName) {
    $FontName = [IO.Path]::GetFileNameWithoutExtension($texture).Substring(2, $texture.Length - 6)
}

Write-Output @"
Font {
    Name: "$FontName";
    Texture: @"./$Texture";
    Characters: {
"@

Import-CSV $InputFile | Sort -Property {[int]$_.id} | % {
    Write-Output "`t`t$($_.id): [$($_.x) $($_.y) $($_.width) $($_.height) $($_.xoffset) $($_.yoffset) $($_.xadvance)];"
}

Write-Output @"
    };
};
"@