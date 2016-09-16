$Dir = get-childitem $PSScriptRoot -recurse
$List = $Dir | where {$_.extension -eq ".fx"}

$exe = 'C:\Program Files (x86)\MSBuild\MonoGame\v3.0\Tools\2MGFX.exe'

# DX 11
New-Item -Force -ItemType directory -Path DX11
$List | % {& "$exe" "$($_.FullName)" "$([io.path]::Combine($_.DirectoryName, 'DX11', $([io.path]::ChangeExtension($_.Name, 'mgfx'))))" '/Profile:DirectX_11' }

# Requires SM3.0 or less
New-Item -Force -ItemType directory -Path OGL
#$List | % {& "$exe" "$($_.FullName)" "$([io.path]::Combine($_.DirectoryName, 'OGL', $([io.path]::ChangeExtension($_.Name, 'mgfx'))))" '/Profile:OpenGL' }