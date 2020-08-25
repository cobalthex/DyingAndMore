param
(
    [switch]$FNA,
    [switch]$DX11,
    [switch]$OpenGL,
    [io.FileInfo[]]$Files = (Get-ChildItem -File -Recurse -Filter "*.fx"),
    [Switch]$FlattenHierarchy,
    [ValidateSet("Debug", "Release")][string]$Configuration = "Release"
)

$currentPath = (Get-Location | Get-Item)

#todo: test if newer (maybe -Force?)
if ($FNA)
{
    Write-Verbose "Compiling for FNA ($Configuration)"

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

$mgfxExe = '../../Tools/mgfxc/mgfxc.exe'

if ($DX11)
{
    Write-Verbose "Compiling for DirectX 11 ($Configuration)"

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

if ($OpenGL)
{
    Write-Verbose "Compiling for OpenGL ($Configuration)"

    $outDir = New-Item -Force -ItemType directory -Path 'OpenGL'
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

if (!$DX11 -and !$OpenGL -and !$FNA)
{
    throw "No build system specified."
}
