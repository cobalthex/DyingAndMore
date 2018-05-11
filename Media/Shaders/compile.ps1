param
(
    [switch]$FNA,
    [switch]$DX11,
    [switch]$OGL,
    [string]$Path = $PSScriptRoot,
    [ValidateSet("Debug", "Release")][string]$Configuration = "Release"
)

$dir = Get-ChildItem $Path -Recurse -Filter "*.fx"


#todo: test if newer (maybe -Force?)
if ($FNA)
{
    Write-Host "Compiling FNA Shaders"
    New-Item -Force -ItemType directory -Path (Join-Path $Path 'FNA') | Out-Null
    $dir | % {
        & "C:\Program Files (x86)\Windows Kits\10\bin\x64\fxc.exe" `
            $(If ($Configuration -eq 'Debug') { '/Od /Zi' }) `
            '/Tfx_4_0' `
            '/Gec' `
            "/Fo$([io.path]::Combine($_.DirectoryName, 'FNA', $([io.path]::ChangeExtension($_.Name, 'fxc'))))" `
            "$($_.FullName)"
        if (!$?) {
            throw
        }
    }
}

$exe = 'C:\Program Files (x86)\MSBuild\MonoGame\v3.0\Tools\2MGFX.exe'

if ($DX11)
{
    Write-Host "Compiling DirectX 11 Shaders"
    New-Item -Force -ItemType directory -Path (Join-Path $Path 'DX11') | Out-Null
    $dir | % {
        & "$exe" `
            "$($_.FullName)" `
            "$([io.path]::Combine($_.DirectoryName, 'DX11', $([io.path]::ChangeExtension($_.Name, 'mgfx'))))" `
            '/Profile:DirectX_11' `
            $(If ($Configuration -eq 'Debug') { '/Debug' })
        if (!$?) {
            throw
        }
    }
}

if ($OGL)
{
    Write-Host "Compiling OpenGL Shaders"
    # Requires SM3.0 or less (in shader file technique)
    New-Item -Force -ItemType directory -Path (Join-Path $Path 'OGL') | Out-Null
    $dir | % {
        & "$exe" `
            "$($_.FullName)" `
            "$([io.path]::Combine($_.DirectoryName, 'OGL', $([io.path]::ChangeExtension($_.Name, 'mgfx'))))" `
            '/Profile:OpenGL' `
            $(If ($Configuration -eq 'Debug') { '/Debug' })
        if (!$?) {
            throw
        }
    }
}

if (!$DX11 -and !$OGL -and !$FNA)
{
    throw "No build system specified."
}
