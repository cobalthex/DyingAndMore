# copy file and any referenced files into a folder

Param(
    [Parameter(Mandatory=$true)][string[]]$File, #file to pack all nested references from
    [string]$Root = (Get-Location),
    [string]$Destination #a folder to copy the refs into (optional)
)

$refs = [Collections.Generic.HashSet[string]]::New()
$queue = [Collections.Generic.Queue[string]]::New($File)

while ($queue.Count -gt 0) {
    $front = $queue.Dequeue()
    if ($refs.Contains($front)) { continue }
    
    $refs.Add((Resolve-Path -Relative $front)) | Out-Null

    if ($front -inotmatch "`.tk$") { continue }
    
    (Select-String -Path (join-Path $Root $front) -Pattern "@[`"`'](.*?)[`"`']") | % { $_.Matches.Groups[1].Value } | Select-Object -Unique | % { 
        $_ = $_.ToString();
        if ($_ -match "^`.[\\/]") {
            $_ = (Join-Path (Split-Path -Parent $front) $_)
        }
        elseif ($_ -ne '.') {
            $queue.Enqueue($_)
        }
    }
}

if ($Destination) {
    New-Item -Type Directory -Force $Destination | Out-Null
    $refs | % {
        New-Item -Type Directory -Force (Join-Path $Destination (Split-Path -parent $_)) | Out-Null # there should be a better way to do this
        Copy-Item -Container -Path $_ -Destination (Join-Path $Destination $_)
    }
}

$refs