param
(
    [switch]$FNA,
    [switch]$DX11,
    [switch]$OGL,
    [io.FileInfo[]]$Files = (Get-ChildItem -File -Recurse -Filter "*.fx"),
    [Switch]$FlattenHierarchy,
    [ValidateSet("Debug", "Release")][string]$Configuration = "Release"
)

$currentPath = (Get-Location | Get-Item)

#todo: test if newer (maybe -Force?)
if ($FNA)
{
    Write-Verbose "Compiling for FNA"

    $outDir = New-Item -Force -ItemType directory -Path 'FNA'
    $Files | Get-Item | % {
        if ($FlattenHierarchy -or (Test-Path -PathType Leaf -Path $_.Name)) {
            $subDir = $outDir
        }
        else { 
            # support nested folders
            $subDir = New-Item -Force -ItemType directory -Path (Join-Path $outDir (Resolve-Path -Relative -Path $_.DirectoryName))
        }

        & "C:\Program Files (x86)\Windows Kits\10\bin\x64\fxc.exe" `
            $(If ($Configuration -eq 'Debug') { '/Od /Zi' }) `
            '/Tfx_4_0' `
            '/Gec' `
            "/Fo$(Join-Path $subDir ([io.path]::ChangeExtension($_.Name, 'fxc')))" `
            "$($_.FullName)"
        if (!$?) {
            throw "Error compiling FNA shader '$_'"
        }
    }
}

$mgfxExe = 'C:\Program Files (x86)\MSBuild\MonoGame\v3.0\Tools\2MGFX.exe'

if ($DX11)
{
    Write-Verbose "Compiling for DirectX 11"
    
    $outDir = New-Item -Force -ItemType directory -Path 'DX11'
    $Files | Get-Item | % {
        if ($FlattenHierarchy -or (Test-Path -PathType Leaf -Path $_.Name)) {
            $subDir = $outDir
        }
        else { 
            # support nested folders
            $subDir = New-Item -Force -ItemType directory -Path (Join-Path $outDir (Resolve-Path -Relative -Path $_.DirectoryName))
        }

        & "$mgfxExe" `
            "$($_.FullName)" `
            "$(Join-Path $subDir ([io.path]::ChangeExtension($_.Name, 'mgfx')))" `
            '/Profile:DirectX_11' `
            $(If ($Configuration -eq 'Debug') { '/Debug' })
        if (!$?) {
            throw "Error compiling DirectX 11 shader '$_'"
        }
    }
}

if ($OGL)
{
    Write-Verbose "Compiling for OpenGL"
    
    $outDir = New-Item -Force -ItemType directory -Path 'OGL'
    $Files | Get-Item | % {
        if ($FlattenHierarchy -or (Test-Path -PathType Leaf -Path $_.Name)) {
            $subDir = $outDir
        }
        else { 
            # support nested folders
            $subDir = New-Item -Force -ItemType directory -Path (Join-Path $outDir (Resolve-Path -Relative -Path $_.DirectoryName))
        }

        & "$mgfxExe" `
            "$($_.FullName)" `
            "$(Join-Path $subDir ([io.path]::ChangeExtension($_.Name, 'mgfx')))" `
            '/Profile:OpenGL' `
            $(If ($Configuration -eq 'Debug') { '/Debug' })
        if (!$?) {
            throw "Error compiling OpenGL shader '$_'"
        }
    }
}

if (!$DX11 -and !$OGL -and !$FNA)
{
    throw "No build system specified."
}
