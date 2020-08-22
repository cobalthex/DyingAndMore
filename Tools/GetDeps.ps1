# copy file and any referenced files into a folder

Param(
    [Parameter(Mandatory=$true)][string[]]$File, #file to pack all nested references from
    [string]$Root = (Get-Location)
)

$File = Resolve-Path $File
Push-Location $Root

$refs = [Collections.Generic.HashSet[string]]::New()
$queue = [Collections.Generic.Queue[string]]::New()
$queue.Enqueue((Resolve-Path -Relative $File))

while ($queue.Count -gt 0) {
    $front = $queue.Dequeue()
    if ($refs.Contains($front)) { continue }

    $refs.Add((Resolve-Path -Relative $front)) | Out-Null

    if ($front -inotmatch "`.tk$") { continue }

    (Select-String -Path $front -Pattern "@[`"`'](.*?)[`"`']") | % { $_.Matches.Groups[1].Value } | Select-Object -Unique | % {
        $_ = $_.ToString();
        if ($_ -match "^`.[\\/]") {
            $_ = (Join-Path (Split-Path -Parent $front) $_)
        }
        elseif ($_ -ne '.') {
            $queue.Enqueue($_)
        }
    }
}
Pop-Location

$refs | % { Join-Path -Resolve $Root $_ }