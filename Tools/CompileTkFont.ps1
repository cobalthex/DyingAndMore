[CmdletBinding()]

Param(
    [Parameter(Mandatory = $true, Position = 0)]
    [IO.FileInfo]$InputBitmap,
    [Parameter(Mandatory = $true, Position = 1)]
    [IO.FileInfo]$InputMetadata,
    [Parameter(Mandatory = $true, Position = 2)]
    [IO.FileInfo]$OutputFile
)

@"
BitmapFont {
`tTexture: @'./$($InputBitmap.Name)';
`tTracking: [0 0];
`tCharacters: {
"@ > $OutputFile

Get-Content -Path $InputMetadata | % {
    $n = [Convert]::ToInt32($_[0])
    $r = $_.SubString(2) -replace "[`0\s]+",' ' #normalize whitespace
    "`t`t$($n): [$r];" >> $OutputFile
}

@"
`t}
}
"@ >> $OutputFile