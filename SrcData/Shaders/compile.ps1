$Dir = get-childitem $PSScriptRoot -recurse
$List = $Dir | where {$_.extension -eq ".fx"}

$exe = 'C:\Program Files (x86)\MSBuild\MonoGame\v3.0\Tools\2MGFX.exe'
$List | % {& "$exe" "$($_.FullName)" "$([io.path]::ChangeExtension($_.FullName, 'mgfx'))" '/Profile:DirectX_11' }