param
(
    [switch]$DX11,
    [switch]$OGL,
    [string]$Path = $PSScriptRoot
)

$Dir = get-childitem $Path -Recurse -Filter "*.fx"

$exe = 'C:\Program Files (x86)\MSBuild\MonoGame\v3.0\Tools\2MGFX.exe'

#todo: test if newer (maybe -Force?)

if ($DX11)
{
    New-Item -Force -ItemType directory -Path DX11 | Out-Null
    $Dir | % {& "$exe" "$($_.FullName)" "$([io.path]::Combine($_.DirectoryName, 'DX11', $([io.path]::ChangeExtension($_.Name, 'mgfx'))))" '/Profile:DirectX_11' }
}
if ($OGL)
{
    # Requires SM3.0 or less
    New-Item -Force -ItemType directory -Path OGL | Out-Null
    $Dir | % {& "$exe" "$($_.FullName)" "$([io.path]::Combine($_.DirectoryName, 'OGL', $([io.path]::ChangeExtension($_.Name, 'mgfx'))))" '/Profile:OpenGL' }
}

if (!$DX11 -and !$OGL)
{
    throw "No build system specified."
}
