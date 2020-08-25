# Find all references in Tk files and make sure that they are case-correct

Param(
    $Path = (Get-Location)
)

Push-Location $Path

$allFiles = @{}
Get-ChildItem -Recurse -Path * -File -Include '*.tk' | % {
    $rel = (Resolve-Path -Relative $_)
    $rel = $rel.Substring(2).Replace('\', '/')
    $allFiles[$rel] = $rel
}

Pop-Location $Path



$allFiles