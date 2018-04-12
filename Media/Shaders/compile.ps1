param
(
    [switch]$DX11,
    [switch]$OGL,
    [string]$Path = $PSScriptRoot,
    [ValidateSet("Debug", "Release")][string]$Configuration = "Release"
)

$dir = Get-ChildItem $Path -Recurse -Filter "*.fx"

$exe = 'C:\Program Files (x86)\MSBuild\MonoGame\v3.0\Tools\2MGFX.exe'

#todo: test if newer (maybe -Force?)

if ($DX11)
{
    New-Item -Force -ItemType directory -Path (Join-Path $Path 'DX11') | Out-Null
    $dir | % {& "$exe" "$($_.FullName)" "$([io.path]::Combine($_.DirectoryName, 'DX11', $([io.path]::ChangeExtension($_.Name, 'mgfx'))))" '/Profile:DirectX_11' $(If ($Configuration -eq 'Debug') {'/Debug'}) }
}
if ($OGL)
{
    # Requires SM3.0 or less (in shader file technique)
    New-Item -Force -ItemType directory -Path (Join-Path $Path 'OGL') | Out-Null
    $dir | % {& "$exe" "$($_.FullName)" "$([io.path]::Combine($_.DirectoryName, 'OGL', $([io.path]::ChangeExtension($_.Name, 'mgfx'))))" '/Profile:OpenGL' $(If ($Configuration -eq 'Debug') {'/Debug'}) }
}

if (!$DX11 -and !$OGL)
{
    throw "No build system specified."
}
