# Find all references in Tk files and make sure that they are case-correct

Param(
    $Path = (Get-Location)
)

Push-Location $Path

$queue = Get-ChildItem -Recurse -Path * -File -Include '*.tk'
$allFiles = @{}
Get-ChildItem -Recurse -Path * -File | % {
    $rel = (Resolve-Path -Relative $_)
    $rel = $rel.Substring(2).Replace('\', '/')
    $allFiles[$_] = $rel
}

Pop-Location

# not efficient
$queue | ForEach-Object -Parallel {
    foreach ($kv in $allFiles.GetEnumerator()) {
        $p = $kv.Value
        $lp = './' + $kv.Key.Name
        # -raw is not ideal, but out-file -NoNewLine will remove all newlines if done per-line
        $s = (Get-Content -LiteralPath $_ -raw) -replace "@([`"'])$p[`"']","@`$1$p`$1"
        $s = $s -replace "@([`"'])$lp[`"']","@`$1$lp`$1"
        $s | out-file -Encoding utf8 -NoNewLine $_
    }
}

#$allFiles