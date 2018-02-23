param(
    [Parameter(Mandatory=$true, Position=1)][string]$MetadataFile,
    [Parameter(Mandatory=$true, Position=2)][string]$ImageFile,
    [Parameter(Mandatory=$true, Position=3)][string]$OutputFile
)

# make sure input files are in UTF8 form

@"
BitmapFont {
"@ +
"`n`tTexture: @'$($ImageFile -replace '\\','/')';`n" +
@"
    Tracking: [0 0];
    Characters: {
"@ > $OutputFile

Get-Content -Encoding UTF8 -Path $MetadataFile | % {
  $s = $_.ToString()
  $tokens = $s.Substring(2) -split ' '
  "`t`t$([int]$s[0]): [$($tokens -join ' ')];" >> $OutputFile
}
       
@"
    }
}
"@ >> $OutputFile